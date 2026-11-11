import { HandView, MeldView } from '../../core/lobby/lobby.models';

/** Cards that went onto the table or came back off it, and whose they are. */
export interface MeldCrossing {
  readonly ids: readonly number[];
  readonly seat: number;
}

/**
 * What moved between two tellings of a table. Every player is told the same
 * thing and works this out for themselves, which is why they all see the
 * same cards travel - and why the player whose move it was is not a special
 * case anywhere.
 */
export interface Crossings {
  readonly meld: MeldCrossing | null;

  /** The seat a card was thrown from, or null where none was. */
  readonly thrownFrom: number | null;
}

const Nothing: Crossings = { meld: null, thrownFrom: null };

export function whatCrossed(before: HandView | null, now: HandView | null): Crossings {
  // Opening the page onto a hand already under way, and a new deal sweeping
  // the table, are both the table being drawn rather than cards crossing it.
  if (before === null || now === null || now.phase !== 'playing') {
    return Nothing;
  }

  return {
    meld: meldCrossed(now.melds, before.melds) ?? meldCrossed(before.melds, now.melds),
    // A card on the pile that ends a turn was thrown; one that does not is
    // a player taking back what they picked up off it, which is the same
    // card going back where it came from and travels nowhere.
    thrownFrom:
      now.discardCount > before.discardCount && now.turnSeat !== before.turnSeat
        ? before.turnSeat
        : null,
  };
}

/** The cards in the first that the second has not, and the seat they are of. */
function meldCrossed(these: readonly MeldView[], those: readonly MeldView[]): MeldCrossing | null {
  const already = new Set(those.flatMap((meld) => meld.cards.map((card) => card.id)));
  const meld = these.find((laid) => laid.cards.some((card) => !already.has(card.id)));

  return meld === undefined ? null : { ids: meld.cards.map((card) => card.id), seat: meld.seat };
}
