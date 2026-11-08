/**
 * The two ways a rummy hand is worth holding. A run is three of one suit in
 * ascending order and a set is three of one rank, and no single order shows
 * both - which is why a hand is sorted by pressing twice rather than once.
 */
export type HandOrder = 'byRank' | 'bySuit';

/** Clubs, diamonds, hearts, spades, as the cards name themselves. */
const Suits = 'CDHS';

const isJoker = (code: string) => code.startsWith('J-');

const rankOf = (code: string) => Number(code.slice(2));
const suitOf = (code: string) => Suits.indexOf(code[0]);

type Key = (code: string) => number;

/**
 * Lowest first, which is how the hand is read: left to right, the two at
 * one end and the ace at the other.
 */
function compare(left: string, right: string, first: Key, then: Key): number {
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
export function inOrder(cards: readonly string[], order: HandOrder): readonly string[] {
  const first = order === 'byRank' ? rankOf : suitOf;
  const then = order === 'byRank' ? suitOf : rankOf;

  return [...cards].sort((left, right) => compare(left, right, first, then));
}
