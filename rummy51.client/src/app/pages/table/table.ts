import { ChangeDetectionStrategy, Component, ElementRef, Injector, OnDestroy, afterNextRender, computed, effect, inject, input, signal, untracked, viewChild, viewChildren } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthService } from '../../core/auth/auth-service';
import { ApiError } from '../../core/errors/api-error';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { ArrangedHand } from '../../core/cards/arranged-hand';
import { HeldCard } from '../../core/cards/card';
import { DrawnFrom, HandPhase, HandView } from '../../core/lobby/lobby.models';
import { TableService } from '../../core/lobby/table-service';
import { Turn } from './turn';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { CardJourney } from './card-journey';
import { Dealing, Spots } from './dealing';
import { CardMove, CardRelease } from './player-hand/hand-drag';
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
  private readonly injector = inject(Injector);

  protected readonly errors = signal<readonly ApiError[]>([]);

  protected readonly hand = this.lobby.hand;

  protected readonly myCards = this.lobby.myCards;

  /** The hand as its owner has arranged it, which is theirs and not the server's. */
  protected readonly arranged = new ArrangedHand(this.myCards);

  protected sortHand(): void {
    this.arranged.sort();
  }

  protected moveCard(move: CardMove): void {
    this.arranged.move(move.ids, move.to, move.firstLine);
  }

  private readonly theCut = viewChild<TableCut>('theCut');
  private readonly pile = viewChild<ElementRef<HTMLElement>>('pile');
  private readonly discardPile = viewChild<ElementRef<HTMLElement>>('discardPile');
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

  /** One card's journey between a pile and the hand, drawn or thrown. */
  private readonly journey = new CardJourney({
    stock: () => this.pile()?.nativeElement,
    discards: () => this.discardPile()?.nativeElement,
    cardAt: (id) => this.nearHand()?.cardAt(id),
  });

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

  /**
   * How long this seat has left to act, and out of how long. Null for
   * every seat but the one being waited for, which is most of them.
   */
  protected clockFor(seat: number): { readonly left: number; readonly of: number } | null {
    const waiting = this.hand()?.waiting ?? null;

    return waiting === null || waiting.seat !== seat
      ? null
      : { left: waiting.secondsLeft, of: waiting.seconds };
  }

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
      return this.arranged.cards();
    }

    // A joker the cut gave the cutter is face up for the deal and no
    // longer. By the time play begins the whole table has had a hand's
    // worth of time to see it, and a card shown for ever is one more thing
    // to carry through every sort, every drag and every move after it.
    const dealing = this.hand()?.phase === 'dealing';

    return dealing && seat === this.cutterSeat() ? this.cutJokers() : Nothing;
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

  /** What this player may do in the hand as it stands, for the screen to read. */
  protected readonly turn = new Turn(
    this.hand,
    this.ownSeat,
    this.show.animating,
    this.arranged.picked,
  );

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
        this.arranged.forget();
      }

      const hand = this.hand();

      if (before === 'cutting' && phase === 'dealing' && hand !== null) {
        this.show.begin(hand);
      }
    });

    // A card drawn is flown in once it is lying where it will lie, which is
    // one render after the hand has heard of it. By then it is also the card
    // standing out of the hand, which is how it is found: a card just drawn
    // is picked the moment it lands. Nothing happens here unless a draw was
    // asked for, so a sort or a throw passes straight through.
    effect(() => {
      this.arranged.cards();

      const landed = untracked(() => this.arranged.picked());

      afterNextRender(() => this.journey.arrived(landed), { injector: this.injector });
    });
  }

  ngOnDestroy(): void {
    this.show.stop();
    this.journey.land();
    void this.lobby.unwatch(this.tableId());
  }

  protected cut(position: number): void {
    this.send(this.tables.cut(this.tableId(), position));
  }

  protected draw(from: DrawnFrom): void {
    this.journey.leaving(from);
    this.send(this.tables.draw(this.tableId(), from));
  }

  private throwCard(cardId: number): void {
    this.journey.thrown(cardId);
    this.send(this.tables.discard(this.tableId(), cardId));
  }

  /**
   * One pile and two moves, never both at once: until a card has been taken
   * the pile is something to take from, and afterwards something to throw
   * onto.
   */
  protected pileClicked(): void {
    if (this.turn.canTakeDiscard()) {
      this.draw('discards');

      return;
    }

    const picked = this.arranged.picked();

    if (this.turn.canThrow() && picked !== null) {
      this.throwCard(picked);
    }
  }

  /**
   * A card let go of outside the hand. One place out there means something,
   * and one card at a time can be thrown onto it.
   */
  protected cardReleased(release: CardRelease): void {
    const pile = this.discardPile()?.nativeElement.getBoundingClientRect();

    if (!this.turn.toThrow() || release.ids.length !== 1 || pile === undefined) {
      return;
    }

    const { x, y } = release.at;

    if (x >= pile.left && x <= pile.right && y >= pile.top && y <= pile.bottom) {
      this.throwCard(release.ids[0]);
    }
  }

  /**
   * A move, and nothing to do with the answer. The hub tells everyone at the
   * table what came of it, this page included, so the only thing the reply
   * is good for here is the complaint.
   */
  private send(move: Observable<HandView>): void {
    this.errors.set([]);

    move.subscribe({
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
