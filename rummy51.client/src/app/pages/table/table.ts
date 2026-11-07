import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, computed, effect, inject, input, signal, viewChild, viewChildren } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { ApiError } from '../../core/errors/api-error';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { HandPhase } from '../../core/lobby/lobby.models';
import { TableService } from '../../core/lobby/table-service';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { TableSeat } from './table-seat/table-seat';

/**
 * How many rows the deck is laid out in for the cut. Eighteen cards to a row
 * at most, which is what leaves each of them enough width to be told apart
 * and aimed at.
 */
const FanRows = 6;

/** The card the cutter stopped on turns over where it lies. */
const Turn = 350;
const SweepAfter = Turn + 80;

/** Then the rest of the deck gathers: one card's journey, and the gap between two. */
const Travel = 240;
const Stagger = 3;

/** Last of all, the finishing card turns a quarter and goes to its place. */
const Place = 380;

/**
 * The table, before and after it fills. Nothing changes screens when the last
 * seat is taken: the felt between the seats is already everything left over on
 * the screen, and that is where the melds will go. The player's own place is
 * the near one, where their hand will sit beneath their name.
 */
@Component({
  selector: 'app-table',
  imports: [TranslatePipe, ErrorMessages, PageShell, ProfileMenu, TableSeat],
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

  /** True from the cut until the deck has settled, which keeps both on screen. */
  protected readonly collapsing = signal(false);

  /** The cards the cut turned up are showing their faces where they lie. */
  protected readonly turning = signal(false);

  private readonly fanCards = viewChildren<ElementRef<HTMLElement>>('fanCard');
  private readonly pile = viewChild<ElementRef<HTMLElement>>('pile');
  private readonly finishingSlot = viewChild<ElementRef<HTMLElement>>('finishingSlot');
  private readonly timers: number[] = [];
  private seenPhase: HandPhase | null = null;

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

  /**
   * Which positions in the fan turned out to be something, and what. Jokers
   * come first from where the cutter stopped and the finishing card after
   * them, wrapping past the end of the deck exactly as the cut itself does.
   */
  private readonly revealedAt = computed<ReadonlyMap<number, string>>(() => {
    const hand = this.hand();

    if (!hand || hand.cutAt === null) {
      return new Map();
    }

    const start = hand.cutAt;
    const found = new Map<number, string>();

    hand.revealedAtCut.forEach((code, step) => found.set((start + step) % hand.deckSize, code));

    if (hand.finishingCard) {
      found.set((start + hand.revealedAtCut.length) % hand.deckSize, hand.finishingCard);
    }

    return found;
  });

  /** Where in the fan the finishing card is lying, before it is sent away. */
  private readonly finishingPosition = computed(() => {
    const hand = this.hand();

    return hand === null || hand.cutAt === null
      ? null
      : (hand.cutAt + hand.revealedAtCut.length) % hand.deckSize;
  });

  /** The face a position turned up, or null while it is still face down. */
  protected faceAt(position: number): string | null {
    return this.revealedAt().get(position) ?? null;
  }

  /** -1 until a hand has begun, which no seat ever matches. */
  protected readonly dealerSeat = computed(() => this.hand()?.dealerSeat ?? -1);

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

  protected readonly perRow = computed(() => Math.ceil((this.hand()?.deckSize ?? 0) / FanRows));

  /**
   * The deck laid out for the cut. One row is unclickable on a phone - a
   * hundred cards across three hundred pixels is three pixels each - so it
   * is folded into the height there is.
   */
  protected readonly fan = computed(() => {
    const size = this.hand()?.deckSize ?? 0;
    const perRow = this.perRow();

    return Array.from({ length: FanRows }, (_, row) =>
      Array.from(
        { length: Math.max(0, Math.min(perRow, size - row * perRow)) },
        (_, column) => row * perRow + column,
      ),
    ).filter((row) => row.length > 0);
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
      if (before === 'cutting' && phase === 'dealing') {
        this.sweepIntoThePile();
      }
    });
  }

  ngOnDestroy(): void {
    this.timers.forEach((timer) => window.clearTimeout(timer));
    void this.lobby.unwatch(this.tableId());
  }

  /**
   * Three beats, in the order a hand is actually cut. The card the cutter
   * stopped on turns over where it lies; then the rest of the deck gathers
   * itself into the pile; and only then does that card turn a quarter and
   * take its place beside it.
   */
  private sweepIntoThePile(): void {
    // Whoever turned animation off in their system meant it here too; the
    // deck simply is in the pile.
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return;
    }

    this.collapsing.set(true);

    // Two frames: one for the cards to be drawn face down before they are
    // asked to turn, and so the board exists to be measured against.
    requestAnimationFrame(() => {
      this.turning.set(true);

      requestAnimationFrame(() => {
        const cards = this.fanCards();
        const revealed = this.revealedAt();

        if (cards.length === 0) {
          this.endSweep();
          return;
        }

        const facedown = cards.filter((_, position) => !revealed.has(position));
        const sweeping = facedown.length * Stagger + Travel;

        this.after(SweepAfter, () => this.gatherToPile(facedown));
        this.after(SweepAfter + sweeping, () => this.sendTheFinishingCard(cards));
        this.after(SweepAfter + sweeping + Place, () => this.endSweep());
      });
    });
  }

  /** Everything still face down, from the far end of the deck back. */
  private gatherToPile(cards: readonly ElementRef<HTMLElement>[]): void {
    const target = this.pile()?.nativeElement.getBoundingClientRect();

    if (!target) {
      return;
    }

    cards.forEach((card, index) => {
      const element = card.nativeElement;
      const from = element.getBoundingClientRect();

      element.style.transitionDuration = `${Travel}ms`;
      element.style.transitionDelay = `${(cards.length - 1 - index) * Stagger}ms`;
      element.style.transform =
        `translate(${target.left - from.left}px, ${target.top - from.top}px)`;
    });
  }

  /**
   * Matched by their centres, because the place it is going to is already
   * turned and its box is the box of a turned card.
   */
  private sendTheFinishingCard(cards: readonly ElementRef<HTMLElement>[]): void {
    const slot = this.finishingSlot()?.nativeElement.getBoundingClientRect();
    const where = this.finishingPosition();
    const element = where === null ? undefined : cards[where]?.nativeElement;

    if (!slot || !element) {
      return;
    }

    const from = element.getBoundingClientRect();

    element.style.zIndex = '20';
    element.style.transitionDuration = `${Place}ms`;
    element.style.transitionDelay = '0ms';
    element.style.transform =
      `translate(${slot.left + slot.width / 2 - from.left - from.width / 2}px, `
      + `${slot.top + slot.height / 2 - from.top - from.height / 2}px) rotate(90deg)`;

    // A joker at the cut belongs to the cutter and is listed on the board, so
    // the one lying in the fan only has to stop being there.
    cards.forEach((card, position) => {
      if (position !== where && this.revealedAt().has(position)) {
        card.nativeElement.style.transitionDuration = `${Place}ms`;
        card.nativeElement.style.opacity = '0';
      }
    });
  }

  private after(delay: number, run: () => void): void {
    this.timers.push(window.setTimeout(run, delay));
  }

  private endSweep(): void {
    this.collapsing.set(false);
    this.turning.set(false);
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
