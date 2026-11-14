import { Signal, computed } from '@angular/core';
import { HandView } from '../../core/lobby/lobby.models';

/**
 * What the player at this screen may do, rule 3.2 leaving only two things a
 * turn can be waiting for. It decides nothing - the server does that and
 * refuses what it disagrees with - and exists so that a pile lights only
 * when pressing it would do something.
 */
export class Turn {
  constructor(
    private readonly hand: Signal<HandView | null>,
    private readonly seat: Signal<number>,
    private readonly animating: Signal<boolean>,
    private readonly picked: Signal<number | null>,
  ) {}

  /** True while the table is waiting for this player rather than another. */
  readonly mine = computed(() => {
    const hand = this.hand();

    return hand?.phase === 'playing' && this.seat() === hand.turnSeat;
  });

  /** Not while cards are flying: the screen is not yet the hand. */
  readonly toDraw = computed(() => this.mine() && !this.hand()?.hasDrawn && !this.animating());

  /**
   * Rules 7.1, 7.3, 6.4 - all of which the server answers, none of them
   * being worth a second copy here.
   */
  readonly canTakeFinishing = computed(
    () => this.toDraw() && (this.hand()?.canTakeFinishing ?? false),
  );

  /** Rules 12.1, 12.2, which the server answers for the same reason. */
  readonly canOfferRedeal = computed(
    () => this.mine() && (this.hand()?.canOfferRedeal ?? false),
  );

  /** Rule 3.3: only while a card lies on it. */
  readonly canTakeDiscard = computed(() => this.toDraw() && this.hand()?.discardTop != null);

  /**
   * And the deck only while there is one. A pile that lights up and then
   * refuses the press is a pile that lied, and the count is on the table
   * for exactly this.
   */
  readonly canTakeStock = computed(() => this.toDraw() && (this.hand()?.stockSize ?? 0) > 0);

  /** Their card taken, so all that is left of the turn is to throw one. */
  readonly toThrow = computed(() => this.mine() && (this.hand()?.hasDrawn ?? false));

  /** Needs a card picked out; dragging one there says which by itself. */
  readonly canThrow = computed(() => this.toThrow() && this.picked() !== null);

  /** Rule 3.6. The server keeps the account; only the hand knows it. */
  readonly canUndo = computed(() => this.mine() && (this.hand()?.canUndo ?? false));

  /**
   * Rule 5.7. The places still show where a card would go before it may go
   * there, knowing that being half of deciding what to lay.
   */
  readonly canAdd = computed(() => this.toThrow() && (this.hand()?.canAdd ?? false));

  /** What the table is waiting for, in a line. Null when it waits elsewhere. */
  readonly prompt = computed(() => {
    if (!this.mine() || this.animating()) {
      return null;
    }

    return this.toThrow() ? 'table.turnThrow' : 'table.turnDraw';
  });
}
