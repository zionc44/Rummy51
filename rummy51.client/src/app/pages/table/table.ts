import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, computed, effect, inject, input, signal, viewChild, viewChildren } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { ApiError } from '../../core/errors/api-error';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { HeldCard } from '../../core/cards/card';
import { HandOrder, inOrder, moved } from '../../core/cards/card-order';
import { HandPhase } from '../../core/lobby/lobby.models';
import { TableService } from '../../core/lobby/table-service';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { Dealing, Spots } from './dealing';
import { CardMove } from './player-hand/hand-drag';
import { PlayerHand } from './player-hand/player-hand';
import { TableCut } from './table-cut/table-cut';
import { TableSeat } from './table-seat/table-seat';

/** Shared, so a seat holding nothing is not handed a new array every pass. */
const Nothing: readonly HeldCard[] = [];

/**
 * The table, before and after it fills. Nothing changes screens when the last
 * seat is taken: the felt between the seats is already everything left over on
 * the screen, and that is where the melds will go. The player's own place is
 * the near one, where their hand will sit beneath their name.
 */
@Component({
  selector: 'app-table',
  imports: [
    TranslatePipe,
    ErrorMessages,
    PageShell,
    PlayerHand,
    ProfileMenu,
    TableCut,
    TableSeat,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table.html',
  styleUrl: './table.scss',
})
export class Table implements OnDestroy {
  readonly tableId = input('');

  private readonly auth = inject(AuthService);
  private readonly lobby = inject(LobbyConnection);
  private readonly tables = inject(TableService);
  private readonly router = inject(Router);

  protected readonly errors = signal<readonly ApiError[]>([]);

  protected readonly hand = this.lobby.hand;

  protected readonly myCards = this.lobby.myCards;

  /**
   * The hand as it was dealt, each card with the one thing that tells it
   * from its twin in the other pack: where it arrived.
   */
  private readonly dealt = computed<readonly HeldCard[]>(() =>
    this.myCards().map((code, id) => ({ id, code })),
  );

  /** Null until the hand has been sorted or a card moved by hand. */
  private readonly laid = signal<readonly HeldCard[] | null>(null);

  /**
   * How many cards the player keeps on the first line of their hand. Null
   * is the natural wrap, and it stays null until a card is carried across.
   */
  protected readonly firstLine = signal<number | null>(null);

  /**
   * Your own cards as you have chosen to hold them. Where they sit is yours
   * and nobody else's: the server sends a hand and is never told an order.
   */
  private readonly myHand = computed(() => this.laid() ?? this.dealt());

  /** Which way the last press sorted them, so the next one does the other. */
  private sortedBy: HandOrder = 'bySuit';

  /**
   * Two presses and two orders, because a run and a set are not the same
   * shape: by rank the sets line up, by suit the runs do. Either of them
   * takes over from an arrangement made by hand, which is what asking to
   * have the hand sorted means.
   */
  protected sortHand(): void {
    this.sortedBy = this.sortedBy === 'byRank' ? 'bySuit' : 'byRank';
    this.laid.set(inOrder(this.dealt(), this.sortedBy));
    this.firstLine.set(null);
  }

  /** One card, or a whole run, put down somewhere else in the hand. */
  protected moveCard(move: CardMove): void {
    this.laid.set(moved(this.myHand(), move.ids, move.to));

    if (move.firstLine !== null) {
      this.firstLine.set(move.firstLine);
    }
  }

  private readonly theCut = viewChild<TableCut>('theCut');
  private readonly pile = viewChild<ElementRef<HTMLElement>>('pile');
  private readonly nearHand = viewChild<PlayerHand>('nearHand');
  private readonly otherSpots = viewChildren<TableSeat>('seatSpot');
  private readonly finishingSlot = viewChild<ElementRef<HTMLElement>>('finishingSlot');
  private seenPhase: HandPhase | null = null;

  /** Everything the cut and the deal have to aim at, found in this view. */
  private spots(): Spots {
    return {
      fanCards: () => this.theCut()?.cards() ?? [],
      pile: () => this.pile()?.nativeElement,
      finishingSlot: () => this.finishingSlot()?.nativeElement,
      faceUp: (seat, step) => this.faceUpSpot(seat, step),
      dealt: (seat, step) =>
        this.isMine(seat)
          ? this.nearHand()?.landingSpot(step)
          : this.otherSeatAt(seat)?.pileSpot(),
    };
  }

  /**
   * Where a card going face up at a seat comes to rest. The near place keeps
   * its cards in a hand of its own; every other seat keeps the few it shows
   * beside the picture.
   */
  private faceUpSpot(seat: number, step: number): HTMLElement | undefined {
    return seat === this.nearSeat()
      ? this.nearHand()?.landingSpot(step)
      : this.otherSeatAt(seat)?.landingSpot(step);
  }

  /** The place across the table at that seat, if it is one of them. */
  private otherSeatAt(seat: number): TableSeat | undefined {
    const among = this.others().findIndex((place) => place.at === seat);

    return among === -1 ? undefined : this.otherSpots()[among];
  }

  /**
   * The cut and the deal played out on screen. This page says where things
   * are and that does the rest, so what is left here is what the table is
   * rather than what it is doing.
   */
  private readonly show = new Dealing(this.spots());

  /** True from the cut until the deck has settled, which keeps both on screen. */
  protected readonly collapsing = this.show.collapsing;

  /** Null while the hub has not said, and once the table has closed. */
  protected readonly table = computed(
    () => this.lobby.tables().find((table) => table.tableId === this.tableId()) ?? null,
  );

  /** Told apart from "not heard yet" only once the connection has answered. */
  protected readonly gone = computed(() => this.lobby.connected() && this.table() === null);

  protected readonly seats = computed(() => this.table()?.seats ?? []);

  protected readonly taken = computed(() => this.seats().filter((seat) => seat.playerId).length);

  protected readonly detail = computed(() => {
    const table = this.table();

    return table?.limit == null
      ? null
      : {
          key: table.mode === 'survival' ? 'lobby.points' : 'lobby.hands',
          amount: { '0': String(table.limit) },
        };
  });

  /** What the deck is made of, which matters before a card is dealt. */
  protected readonly deck = computed(() => {
    const table = this.table();

    if (!table) {
      return null;
    }

    return table.jokers === 0
      ? { key: 'lobby.noJokers', amount: {} }
      : { key: 'lobby.withJokers', amount: { '0': String(table.jokers) } };
  });

  /** -1 while the viewer is not sitting at this table. */
  private readonly ownSeat = computed(() => {
    const me = this.auth.currentPlayer()?.playerId ?? null;

    return this.seats().findIndex((seat) => seat.playerId === me);
  });

  protected readonly seated = computed(() => this.ownSeat() !== -1);

  protected readonly nearSeat = computed(() => Math.max(0, this.ownSeat()));

  protected readonly near = computed(() => this.seats()[this.nearSeat()] ?? null);

  /** How far the cut has got in turning its cards over. */
  protected readonly turned = this.show.turned;

  /** -1 until a hand has begun, which no seat ever matches. */
  protected readonly dealerSeat = computed(() => this.hand()?.dealerSeat ?? -1);

  protected readonly cutterSeat = computed(() => this.hand()?.cutterSeat ?? -1);

  /** The place along the bottom, which is the viewer's own only if they sit. */
  private isMine(seat: number): boolean {
    return this.seated() && seat === this.nearSeat();
  }

  /**
   * What a seat shows face up: your own hand at your own place, and at
   * anybody else's only the jokers the cut gave them.
   */
  private faceUpCards(seat: number): readonly HeldCard[] {
    if (this.isMine(seat)) {
      return this.myHand();
    }

    return seat === this.cutterSeat() ? this.cutJokers() : Nothing;
  }

  /** The jokers the cut gave the cutter, which nobody rearranges. */
  private readonly cutJokers = computed<readonly HeldCard[]>(() =>
    (this.hand()?.revealedAtCut ?? []).map((code, id) => ({ id, code })),
  );

  /** Those cards, with a gap kept for each one still on its way. */
  protected heldAt(seat: number): readonly (HeldCard | null)[] {
    const cards = this.faceUpCards(seat);

    // While anything is moving a seat shows what has reached it. Otherwise
    // it shows everything, which is what somebody opening the page
    // afterwards must see: for them nothing is going to arrive.
    if (!this.show.animating()) {
      return cards;
    }

    const landed = this.show.arrivals.reached(seat);
    const upTo = Math.min(this.show.arrivals.reaching(seat), cards.length);

    return upTo === 0
      ? Nothing
      : Array.from({ length: upTo }, (_, step) => (step < landed ? cards[step] : null));
  }

  /**
   * How many cards a seat is holding. Null before the deal, when nobody is
   * holding anything and the answer would only be a nought.
   */
  protected holdingAt(seat: number): number | null {
    const hand = this.hand();

    if (hand === null || hand.phase === 'cutting') {
      return null;
    }

    // Not while the deck is still being swept up: an empty pile at every
    // seat before a card has been dealt is a promise nothing has made yet.
    if (this.show.collapsing() && !this.show.dealing()) {
      return null;
    }

    return this.show.animating()
      ? this.show.arrivals.reached(seat)
      : hand.cardsPerSeat[seat] ?? 0;
  }

  /**
   * How many cards a seat holds once the deal is done. The hand needs it
   * before the cards are there: a line already as wide as it will be does
   * not move the cards in it while the rest are still landing.
   */
  protected placesAt(seat: number): number {
    return this.hand()?.cardsPerSeat[seat] ?? 0;
  }

  /**
   * Everyone else, counting round the table from the player's own seat, so
   * the row across from them reads in the order play will take.
   */
  protected readonly others = computed(() => {
    const seats = this.seats();
    const from = Math.max(0, this.ownSeat());

    return Array.from({ length: Math.max(0, seats.length - 1) }, (_, step) => {
      const at = (from + step + 1) % seats.length;

      return { seat: seats[at], at };
    });
  });

  protected readonly canCut = computed(() => {
    const hand = this.hand();

    return hand?.phase === 'cutting' && this.ownSeat() === hand.cutterSeat;
  });

  protected readonly cutter = computed(() => {
    const hand = this.hand();

    return hand ? this.seats()[hand.cutterSeat]?.displayName ?? '' : '';
  });

  constructor() {
    effect(() => {
      const table = this.tableId();

      // Only once the hub is up: watching is a call to it, not a wish.
      if (this.lobby.connected() && table) {
        void this.lobby.watch(table);
      }
    });

    effect(() => {
      const phase = this.hand()?.phase ?? null;
      const before = this.seenPhase;
      this.seenPhase = phase;

      // The change is the event, not the state. Somebody opening the page
      // after the cut should not be shown it happening again.
      if (phase === 'cutting') {
        this.show.forget();
        this.laid.set(null);
        this.firstLine.set(null);
      }

      const hand = this.hand();

      if (before === 'cutting' && phase === 'dealing' && hand !== null) {
        this.show.begin(hand);
      }
    });
  }

  ngOnDestroy(): void {
    this.show.stop();
    void this.lobby.unwatch(this.tableId());
  }

  protected cut(position: number): void {
    this.errors.set([]);

    this.tables.cut(this.tableId(), position).subscribe({
      // Nothing to do: the hub tells everyone at the table, this page included.
      next: () => undefined,
      error: (errors: ApiError[]) => this.errors.set(errors),
    });
  }

  protected leave(): void {
    this.errors.set([]);

    this.tables.leave().subscribe({
      next: () => void this.router.navigate(['/lobby']),
      error: (errors: ApiError[]) => this.errors.set(errors),
    });
  }

  protected backToLobby(): void {
    void this.router.navigate(['/lobby']);
  }
}
