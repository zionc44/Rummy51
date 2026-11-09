import { HandView } from '../lobby/lobby.models';

/**
 * What the cut turned up, read off the hand the server sent.
 *
 * The cutter clicks a card in the fan. Every joker from there is theirs and
 * is turned over one at a time; the card beneath the last of them is the
 * finishing card. All of it wraps past the end of the deck as the cut
 * itself does, so none of this is a position in a list - it is a position
 * in a ring.
 */

/** Where the cut turned something up, in the order it turns up. */
export function revealedOrder(hand: HandView | null): readonly number[] {
  const from = hand?.cutAt ?? null;

  if (hand === null || from === null) {
    return [];
  }

  const upTo = hand.revealedAtCut.length + (hand.finishingCard ? 1 : 0);

  return Array.from({ length: upTo }, (_, step) => (from + step) % hand.deckSize);
}

/** The same cards by the position they lie at, which is how they are drawn. */
export function revealedAt(hand: HandView | null): ReadonlyMap<number, string> {
  const jokers = hand?.revealedAtCut ?? [];
  const codes = hand?.finishingCard ? [...jokers, hand.finishingCard] : jokers;

  return new Map(revealedOrder(hand).map((position, step) => [position, codes[step]]));
}

/** Where the finishing card is lying, before it is sent to its slot. */
export function finishingPosition(hand: HandView | null): number | null {
  return hand === null || hand.cutAt === null
    ? null
    : (hand.cutAt + hand.revealedAtCut.length) % hand.deckSize;
}

/** The jokers the cut gave the cutter, which go to them before anything else. */
export function jokersAtCut(hand: HandView | null): readonly number[] {
  return revealedOrder(hand).slice(0, hand?.revealedAtCut.length ?? 0);
}
