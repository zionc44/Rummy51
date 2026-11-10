import { Signal, computed } from '@angular/core';
import { HandView } from '../../core/lobby/lobby.models';

/**
 * What the player at this screen may do in the hand as it stands. A turn is
 * a card in and a card out, so there are only ever two things it can be
 * waiting for, and everything here is one of those two asked a slightly
 * different way: may they take one, may they throw one, and which of the two
 * the table is holding its breath over.
 *
 * It decides nothing about the game - the server does that, and refuses
 * anything it disagrees with. What this is for is the screen: a pile that
 * lights only when pressing it would do something, and a line that says
 * whose move it is.
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

  /**
   * Their turn with their half of it still to come. Not while cards are
   * flying: the deal is still being watched, and what is on the screen is
   * not yet what is in the hand.
   */
  readonly toDraw = computed(() => this.mine() && !this.hand()?.hasDrawn && !this.animating());

  /** The pile is only something to take from while a card lies on it. */
  readonly canTakeDiscard = computed(() => this.toDraw() && this.hand()?.discardTop != null);

  /** Their card taken, so all that is left of the turn is to throw one. */
  readonly toThrow = computed(() => this.mine() && (this.hand()?.hasDrawn ?? false));

  /**
   * Whether throwing onto the pile would do anything, which needs a card
   * picked out first. Dragging one there says which card by itself and does
   * not ask this.
   */
  readonly canThrow = computed(() => this.toThrow() && this.picked() !== null);

  /** What the table is waiting for, in a line. Null when it waits elsewhere. */
  readonly prompt = computed(() => {
    if (!this.mine() || this.animating()) {
      return null;
    }

    return this.toThrow() ? 'table.turnThrow' : 'table.turnDraw';
  });
}
