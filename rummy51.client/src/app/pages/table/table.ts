import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, WritableSignal, computed, effect, inject, input, signal, viewChild, viewChildren } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { ApiError } from '../../core/errors/api-error';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { DealPass, HandPhase } from '../../core/lobby/lobby.models';
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

/** A seat nothing has reached yet is not in the list at all. */
function countAt(counts: readonly number[], seat: number): number {
  return counts[seat] ?? 0;
}

/** Each card the cut turns up turns over where it lies, then is read. */
const Turn = 350;
const Read = 80;

/** Then the rest of the deck gathers: one card's journey, and the gap between two. */
const Travel = 240;
const Stagger = 3;

/** A card that leaves the fan - to the cutter, or to the finishing slot. */
const Place = 380;

/** Shared, so a seat holding nothing is not handed a new array every pass. */
const Nothing: readonly string[] = [];

/** The deal: a card's journey, the gap inside a pass, and between two. */
const Pass = 260;
const BetweenCards = 50;
const BetweenPasses = 150;

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

  /** True while the cards are going round the table. */
  protected readonly dealing = signal(false);

  /**
   * Cards that have reached each seat, and cards still on their way there.
   * A card in the air has its place kept for it, so the row it is joining
   * is already the width it will be and nothing shifts as it lands.
   */
  private readonly arrived = signal<readonly number[]>([]);
  private readonly inFlight = signal<readonly number[]>([]);

  /**
   * While anything is moving, a seat shows what has reached it. Otherwise it
   * shows everything - which is what somebody opening the page afterwards
   * must see, since for them nothing is going to arrive.
   */
  private readonly animating = computed(() => this.collapsing() || this.dealing());

  protected readonly myCards = this.lobby.myCards;

  /**
   * How many of the cards the cut turned up have been turned over so far.
   * They go one at a time: a joker is gone to the cutter before the card
   * beneath it is touched, which is the order a hand is really cut in.
   */
  private readonly turned = signal(0);

  private readonly fanCards = viewChildren<ElementRef<HTMLElement>>('fanCard');
  private readonly pile = viewChild<ElementRef<HTMLElement>>('pile');
  private readonly nearSpot = viewChild<TableSeat>('nearSpot');
  private readonly otherSpots = viewChildren<TableSeat>('seatSpot');
  private readonly finishingSlot = viewChild<ElementRef<HTMLElement>>('finishingSlot');
  private readonly timers: number[] = [];
  private readonly flying: HTMLElement[] = [];
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
   * Where in the fan the cut turned something up, in the order it turns up.
   * Jokers come first from where the cutter stopped and the finishing card
   * after them, wrapping past the end of the deck as the cut itself does.
   */
  private readonly revealedOrder = computed<readonly number[]>(() => {
    const hand = this.hand();

    if (!hand || hand.cutAt === null) {
      return [];
    }

    const start = hand.cutAt;
    const upTo = hand.revealedAtCut.length + (hand.finishingCard ? 1 : 0);

    return Array.from({ length: upTo }, (_, step) => (start + step) % hand.deckSize);
  });

  /** The same cards by the position they lie at, which is how they are drawn. */
  private readonly revealedAt = computed<ReadonlyMap<number, string>>(() => {
    const hand = this.hand();
    const jokers = hand?.revealedAtCut ?? [];
    const codes = hand?.finishingCard ? [...jokers, hand.finishingCard] : jokers;

    return new Map(this.revealedOrder().map((position, step) => [position, codes[step]]));
  });

  /** Where in the fan the finishing card is lying, before it is sent away. */
  private readonly finishingPosition = computed(() => {
    const hand = this.hand();

    return hand === null || hand.cutAt === null
      ? null
      : (hand.cutAt + hand.revealedAtCut.length) % hand.deckSize;
  });

  /**
   * The jokers, shown where they lie so a cut onto one can be aimed at. One
   * falls at the cut about twice in a hundred hands, which is no way to work
   * on that path. Empty unless the server was asked to send them.
   */
  protected readonly jokers = computed(() => new Set(this.hand()?.jokerPositions ?? []));

  /** The face a position turned up, or null while it is still face down. */
  protected faceAt(position: number): string | null {
    return this.revealedAt().get(position) ?? null;
  }

  /** Whether the card lying there has had its turn yet. */
  protected isTurned(position: number): boolean {
    const step = this.revealedOrder().indexOf(position);

    return step !== -1 && step < this.turned();
  }

  /**
   * The one card the sequence is on. It belongs above every other, its
   * neighbours in the fan and the cards the cut turned up before it alike,
   * or it turns over half hidden beneath the one lying on top of it.
   */
  protected isTurning(position: number): boolean {
    return this.revealedOrder()[this.turned() - 1] === position;
  }

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
  private faceUpCards(seat: number): readonly string[] {
    if (this.isMine(seat)) {
      return this.myCards();
    }

    return seat === this.cutterSeat() ? this.hand()?.revealedAtCut ?? Nothing : Nothing;
  }

  /** Those cards, with a gap kept for each one still on its way. */
  protected heldAt(seat: number): readonly (string | null)[] {
    const cards = this.faceUpCards(seat);

    if (!this.animating()) {
      return cards;
    }

    const landed = countAt(this.arrived(), seat);
    const upTo = Math.min(landed + countAt(this.inFlight(), seat), cards.length);

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
    if (this.collapsing() && !this.dealing()) {
      return null;
    }

    return this.animating() ? countAt(this.arrived(), seat) : hand.cardsPerSeat[seat] ?? 0;
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
      if (phase === 'cutting') {
        this.arrived.set([]);
        this.inFlight.set([]);
      }

      if (before === 'cutting' && phase === 'dealing') {
        this.sweepIntoThePile();
      }
    });
  }

  ngOnDestroy(): void {
    this.timers.forEach((timer) => window.clearTimeout(timer));
    this.landAll();
    void this.lobby.unwatch(this.tableId());
  }

  /**
   * The cut played out in the order it happens at a table. Every joker the
   * cutter turns over goes to them before the card beneath it is touched;
   * then the rest of the deck gathers itself into the pile; and last of all
   * the finishing card turns a quarter and takes its place beside it.
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
      this.turned.set(1);

      requestAnimationFrame(() => {
        const cards = this.fanCards();

        if (cards.length === 0) {
          this.endSweep();
          return;
        }

        this.scheduleTheCut(cards);
      });
    });
  }

  /** From the first card already turning, laid end to end. */
  private scheduleTheCut(cards: readonly ElementRef<HTMLElement>[]): void {
    const revealed = this.revealedAt();
    const jokers = this.revealedOrder().slice(0, this.hand()?.revealedAtCut.length ?? 0);
    const facedown = cards.filter((_, position) => !revealed.has(position));
    let at = Turn + Read;

    // Every joker's place at the seat is kept from the start, so each one
    // has somewhere measured to fly to when its turn comes.
    this.bump(this.inFlight, this.cutterSeat(), jokers.length);

    jokers.forEach((position, step) => {
      this.after(at, () => this.sendJokerToCutter(cards[position], step));
      at += Place;
      this.after(at, () => {
        this.land(this.cutterSeat(), 1);
        this.turned.set(step + 2);
      });
      at += Turn + Read;
    });

    this.after(at, () => this.gatherToPile(facedown));
    at += facedown.length * Stagger + Travel;
    this.after(at, () => this.sendTheFinishingCard(cards));
    at += Place;
    this.after(at, () => this.endSweep());
  }

  /**
   * A joker at the cut is the cutter's, so it travels to the place waiting
   * for it at their seat and stays there, face up, for the whole table to
   * read until the deal is over.
   */
  private sendJokerToCutter(card: ElementRef<HTMLElement> | undefined, step: number): void {
    const spot = this.cutterSpot(step)?.getBoundingClientRect();
    const element = card?.nativeElement;

    if (!spot || !element) {
      return;
    }

    const from = element.getBoundingClientRect();
    const flying = this.liftOutOfTheFan(element, from);

    // Down to the size of the place it is going to, which is cut to the
    // seat it belongs to and is not the size a card is in the fan.
    const sized = `scale(${spot.width / from.width})`;

    flying.style.transform = `${this.betweenCentres(from, spot)} ${sized}`;
  }

  /**
   * The card leaves the fan as a copy fixed to the window. The fan scrolls,
   * inside a felt that clips, and the seat is outside both: flown as it
   * lies, the card would be cut off at the edge and would raise a scrollbar
   * on its way out. The one it leaves behind keeps its place in the row.
   */
  private liftOutOfTheFan(element: HTMLElement, at: DOMRect): HTMLElement {
    const flying = element.cloneNode(true) as HTMLElement;

    // Window pixels, which have neither a start nor an end to speak of.
    flying.style.position = 'fixed';
    flying.style.left = `${at.left}px`;
    flying.style.top = `${at.top}px`;
    flying.style.width = `${at.width}px`;
    flying.style.margin = '0';
    flying.style.transform = 'none';
    flying.style.transition = `transform ${Place}ms ease-in-out`;
    flying.setAttribute('inert', '');

    element.style.visibility = 'hidden';
    document.body.append(flying);
    this.flying.push(flying);

    // Reading the copy back settles the browser on where it is before it is
    // asked to move. Without that it is given both ends of the journey in
    // one go, has nothing to move from, and arrives without travelling.
    flying.getBoundingClientRect();

    return flying;
  }

  /** The seat's own card is beneath them by now, so the copies can go. */
  private landAll(): void {
    this.flying.splice(0).forEach((card) => card.remove());
  }

  /** Where the joker is going: the place waiting for it at the cutter's seat. */
  private cutterSpot(step: number): HTMLElement | undefined {
    return this.seatAt(this.cutterSeat())?.landingSpot(step);
  }

  private seatAt(seat: number): TableSeat | undefined {
    const among = this.others().findIndex((place) => place.at === seat);

    return seat === this.nearSeat() ? this.nearSpot() : this.otherSpots()[among];
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

  /** The last card left in the fan, and the only one still to be placed. */
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
    element.style.transform = `${this.betweenCentres(from, slot)} rotate(90deg)`;
  }

  /**
   * Centre to centre, because a card lying on its side does not have the box
   * an upright one has and matching their corners would set it adrift.
   */
  private betweenCentres(from: DOMRect, to: DOMRect): string {
    const across = to.left + to.width / 2 - from.left - from.width / 2;
    const down = to.top + to.height / 2 - from.top - from.height / 2;

    return `translate(${across}px, ${down}px)`;
  }

  private after(delay: number, run: () => void): void {
    this.timers.push(window.setTimeout(run, delay));
  }

  private endSweep(): void {
    // The deal follows the cut with nobody deciding anything in between,
    // here as on the server, and it starts before the fan is taken away so
    // there is no still moment between the two.
    this.dealTheHand();
    this.collapsing.set(false);
    this.turned.set(0);
    this.landAll();
  }

  /**
   * The deal as the server counted it out: pass after pass round the table,
   * three cards at a time and fewer where the cut already gave somebody a
   * joker. Nothing here decides who gets what; it only shows it happening.
   */
  private dealTheHand(): void {
    const passes = this.hand()?.passes ?? [];

    if (passes.length === 0) {
      return;
    }

    this.dealing.set(true);

    let at = 0;
    let dealt = 0;

    passes.forEach((pass) => {
      const before = dealt;

      this.after(at, () => this.flyPass(pass, before));
      at += BetweenPasses;
      dealt += pass.count;
    });

    // Clear of the last card landing, so no copy is taken away in the air.
    this.after(at + Pass + BetweenPasses * 2, () => this.endDeal());
  }

  /** One pass: a seat's three cards, one just behind the other. */
  private flyPass(pass: DealPass, dealt: number): void {
    this.bump(this.inFlight, pass.seat, pass.count);

    // Two frames, so the places the cards are going have been laid out
    // before anything measures them.
    requestAnimationFrame(() =>
      requestAnimationFrame(() => {
        const from = this.pile()?.nativeElement.getBoundingClientRect();
        const landed = countAt(this.arrived(), pass.seat);
        const sent: HTMLElement[] = [];

        Array.from({ length: pass.count }, (_, step) => {
          const to = this.landingFor(pass.seat, landed + step)?.getBoundingClientRect();

          if (from && to) {
            sent.push(this.flyCard(this.backOfDealt(dealt + step), from, to, step * BetweenCards));
          }
        });

        this.after(Pass + pass.count * BetweenCards, () => {
          this.land(pass.seat, pass.count);
          // A frame later, so the card it is standing in for is drawn first.
          requestAnimationFrame(() => sent.forEach((card) => card.remove()));
        });
      }),
    );
  }

  /** Face up in your own row, or onto the pile nobody else may look at. */
  private landingFor(seat: number, step: number): HTMLElement | undefined {
    const place = this.seatAt(seat);

    return this.isMine(seat) ? place?.landingSpot(step) : place?.pileSpot();
  }

  /**
   * The back the next card off the deck wears. The deal starts just past the
   * finishing card and runs on round the deck, so each one keeps the back of
   * the pack it came from rather than being given one.
   */
  private backOfDealt(step: number): string {
    const hand = this.hand();

    if (!hand || hand.cutAt === null) {
      return '1B';
    }

    const from = hand.cutAt + hand.revealedAtCut.length + 1 + step;

    return hand.backs[from % hand.deckSize];
  }

  /**
   * A card on its way, fixed to the window. Everything it has to cross
   * clips: the felt hides what leaves it, and the seats are outside.
   */
  private flyCard(back: string, from: DOMRect, to: DOMRect, delay: number): HTMLElement {
    const card = document.createElement('img');

    card.src = `cards/${back}.svg`;
    card.alt = '';
    card.setAttribute('inert', '');

    // Window pixels, which have neither a start nor an end to speak of.
    card.style.position = 'fixed';
    card.style.left = `${from.left}px`;
    card.style.top = `${from.top}px`;
    card.style.width = `${from.width}px`;
    card.style.zIndex = '30';
    card.style.transition = `transform ${Pass}ms ease-in-out ${delay}ms`;

    document.body.append(card);
    this.flying.push(card);

    // Settled where it starts before it is told where to go, or it is given
    // both ends of the journey at once and arrives without travelling.
    card.getBoundingClientRect();
    card.style.transform =
      `${this.betweenCentres(from, to)} scale(${to.width / from.width})`;

    return card;
  }

  private endDeal(): void {
    this.dealing.set(false);
    this.landAll();
  }

  private land(seat: number, count: number): void {
    this.bump(this.inFlight, seat, -count);
    this.bump(this.arrived, seat, count);
  }

  private bump(counts: WritableSignal<readonly number[]>, seat: number, by: number): void {
    counts.update((current) => {
      const next = [...current];

      while (next.length <= seat) {
        next.push(0);
      }

      next[seat] += by;

      return next;
    });
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
