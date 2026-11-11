import { HeldCard, isJoker, rankOf, suitOf } from './card';

/**
 * The two ways a rummy hand is worth holding. A run is three of one suit in
 * ascending order and a set is three of one rank, and no single order shows
 * both - which is why a hand is sorted by pressing twice rather than once.
 */
export type HandOrder = 'byRank' | 'bySuit';

type Key = (card: HeldCard) => number;

/**
 * Lowest first, which is how the hand is read: left to right, the two at
 * one end and the ace at the other.
 */
function compare(left: HeldCard, right: HeldCard, first: Key, then: Key): number {
  // A joker is neither a rank nor a suit, so it belongs at an end rather
  // than anywhere among them - past the highest card there is.
  if (isJoker(left) && isJoker(right)) {
    return 0;
  }

  if (isJoker(left) || isJoker(right)) {
    return isJoker(left) ? 1 : -1;
  }

  return first(left) - first(right) || then(left) - then(right);
}

/**
 * The hand laid out to be read. How it is laid is a view of the cards and
 * not a fact about them: the server sends a hand and is never told an
 * order, so this is the client's own and nobody else's business.
 *
 * The ace counts fourteen, so it sorts beside the king and not the two. It
 * opens A-2-3 as well, and no one line can put it at both ends of a suit.
 */
export function inOrder(cards: readonly HeldCard[], order: HandOrder): readonly HeldCard[] {
  const first = order === 'byRank' ? rankOf : suitOf;
  const then = order === 'byRank' ? suitOf : rankOf;

  return [...cards].sort((left, right) => compare(left, right, first, then));
}

/**
 * Cards picked up and put down somewhere else in the same hand - one of
 * them, or a whole run carried by its band. The place is counted over the
 * cards that are left once they have been taken out, which is what the
 * hand looks like while they are being moved.
 */
export function moved(
  cards: readonly HeldCard[],
  ids: readonly number[],
  to: number,
): readonly HeldCard[] {
  const carried = cards.filter((held) => ids.includes(held.id));

  if (carried.length === 0) {
    return cards;
  }

  const rest = cards.filter((held) => !ids.includes(held.id));

  return [...rest.slice(0, to), ...carried, ...rest.slice(to)];
}

/**
 * The hand the server says you are holding, laid out the way you arranged
 * it. Cards keep the places you gave them, one that has just reached you
 * joins the end, and one that has left takes its place with it.
 *
 * The arrangement is held against what the server sends rather than instead
 * of it, because a hand changes under the player all game: they draw, they
 * throw, and what they decided about the cards in between has to survive it.
 */
export function arrangedLike(
  held: readonly HeldCard[],
  arrangement: readonly HeldCard[] | null,
): readonly HeldCard[] {
  if (arrangement === null) {
    return held;
  }

  const stillHeld = new Set(held.map((card) => card.id));
  const placed = arrangement.filter((card) => stillHeld.has(card.id));
  const known = new Set(placed.map((card) => card.id));

  return [...placed, ...held.filter((card) => !known.has(card.id))];
}

/**
 * An arrangement with the cards that are not in the hand put back where
 * they were lying.
 *
 * A card laid on the table has not left its owner's hand in the sense that
 * matters here: they still know where it was, and when it comes back - a
 * turn taken back - it belongs there and not at the end. So the places they
 * gave their cards are kept for cards that are somewhere else for a while.
 *
 * Each absent card is remembered by the card it was lying after, which
 * survives the hand being rearranged around it in a way an index does not.
 */
export function keepingTheirPlaces(
  order: readonly HeldCard[],
  remembered: readonly HeldCard[],
): readonly HeldCard[] {
  const here = new Set(order.map((card) => card.id));
  const behind = new Map<number | null, HeldCard[]>();

  remembered.forEach((card, at) => {
    if (!here.has(card.id)) {
      const after = at === 0 ? null : remembered[at - 1].id;

      behind.set(after, [...(behind.get(after) ?? []), card]);
    }
  });

  if (behind.size === 0) {
    return order;
  }

  const laid: HeldCard[] = [];

  // Whatever was lying behind this card, and whatever was lying behind
  // that: cards that left together come back together.
  const following = (id: number | null) => {
    for (const card of behind.get(id) ?? []) {
      laid.push(card);
      following(card.id);
    }
  };

  following(null);

  for (const card of order) {
    laid.push(card);
    following(card.id);
  }

  return laid;
}
