import { Signal, computed } from '@angular/core';
import { HeldCard } from '../../core/cards/card';
import { HandView, LobbyTable } from '../../core/lobby/lobby.models';
import { Dealing } from './dealing';

/** Shared, so a seat holding nothing is not handed a new array every pass. */
const Nothing: readonly HeldCard[] = [];

/**
 * Who is at this table as this viewer sees it, and what each of their places
 * shows. One question asked of every seat, which is why it is one thing: a
 * seat is drawn from where it sits relative to the viewer, from what the
 * hand says about it, and from how much of the deal has reached it.
 *
 * It decides nothing and touches no element. The page knows where things
 * are on the screen; this knows what belongs at each of them.
 *
 * The deal is here because a seat shows less while the cards are still
 * going out than it will when they have landed, and only the thing playing
 * that out knows how far it has got.
 */
export class Seating {
  constructor(
    private readonly table: Signal<LobbyTable | null>,
    private readonly hand: Signal<HandView | null>,
    private readonly me: Signal<string | null>,
    private readonly show: Dealing,
    private readonly myHand: Signal<readonly HeldCard[]>,
  ) {}

  readonly seats = computed(() => this.table()?.seats ?? []);

  readonly taken = computed(() => this.seats().filter((seat) => seat.playerId).length);

  /** -1 while the viewer is not sitting at this table. */
  readonly ownSeat = computed(() => {
    const me = this.me();

    return this.seats().findIndex((seat) => seat.playerId === me);
  });

  readonly seated = computed(() => this.ownSeat() !== -1);

  readonly nearSeat = computed(() => Math.max(0, this.ownSeat()));

  readonly near = computed(() => this.seats()[this.nearSeat()] ?? null);

  /**
   * Everyone else, counting round the table from the player's own seat, so
   * the row across from them reads in the order play will take.
   */
  readonly others = computed(() => {
    const seats = this.seats();
    const from = Math.max(0, this.ownSeat());

    return Array.from({ length: Math.max(0, seats.length - 1) }, (_, step) => {
      const at = (from + step + 1) % seats.length;

      return { seat: seats[at], at };
    });
  });

  /** The place along the bottom, which is the viewer's own only if they sit. */
  mine(seat: number): boolean {
    return this.seated() && seat === this.nearSeat();
  }

  /** -1 until a hand has begun, which no seat ever matches. */
  readonly dealerSeat = computed(() => this.hand()?.dealerSeat ?? -1);

  readonly cutterSeat = computed(() => this.hand()?.cutterSeat ?? -1);

  readonly cutter = computed(() => {
    const hand = this.hand();

    return hand ? this.seats()[hand.cutterSeat]?.displayName ?? '' : '';
  });

  /**
   * How long this seat has left to act, and out of how long. Null for
   * every seat but the one being waited for, which is most of them.
   */
  clockFor(seat: number): { readonly left: number; readonly of: number } | null {
    const waiting = this.hand()?.waiting ?? null;

    return waiting === null || waiting.seat !== seat
      ? null
      : { left: waiting.secondsLeft, of: waiting.seconds };
  }

  /**
   * What a seat shows face up: your own hand at your own place, and at
   * anybody else's only the jokers the cut gave them.
   */
  private faceUpCards(seat: number): readonly HeldCard[] {
    if (this.mine(seat)) {
      return this.myHand();
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
  heldAt(seat: number): readonly (HeldCard | null)[] {
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
  holdingAt(seat: number): number | null {
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
  placesAt(seat: number): number {
    return this.hand()?.cardsPerSeat[seat] ?? 0;
  }
}
