import { Ace, HeldCard, King, isJoker, rankOf, suitOf } from './card';

/**
 * Which cards in a line of a hand are lying in a run or a set together.
 *
 * It reads the hand as it is laid rather than searching it for the best
 * arrangement there could be. A card belongs to a meld because the player
 * has put it beside the others, which is why arranging a hand by hand came
 * first - and why a run broken across two lines is not one.
 *
 * Being a meld is not the same as being allowed to go down. That needs a
 * clean run and fifty-one points besides, and it is a different question
 * asked of a different thing.
 */

/** The fewest cards that can be a run or a set. */
const Least = 3;

/** The most that can be a set, there being four suits and no duplicates. */
const Most = 4;

/**
 * Each place in the line given the number of the meld it belongs to, or
 * null where it belongs to none. The numbers only have to differ between
 * neighbours, which is what lets one meld be told from the next.
 */
export function meldsIn(line: readonly (HeldCard | null)[]): readonly (number | null)[] {
  const marks: (number | null)[] = line.map(() => null);
  let found = 0;
  let at = 0;

  while (at < line.length) {
    const length = longestFrom(line, at);

    if (length === 0) {
      at += 1;
      continue;
    }

    for (let card = at; card < at + length; card += 1) {
      marks[card] = found;
    }

    found += 1;
    at += length;
  }

  return marks;
}

/**
 * The longest meld that starts here, because a run of five is one band and
 * not two overlapping threes. Zero where none starts here at all.
 */
function longestFrom(line: readonly (HeldCard | null)[], at: number): number {
  for (let length = line.length - at; length >= Least; length -= 1) {
    const group = line.slice(at, at + length);

    if (group.every((card) => card !== null) && isMeld(group as HeldCard[])) {
      return length;
    }
  }

  return 0;
}

const isMeld = (cards: readonly HeldCard[]) => isSet(cards) || isRun(cards);

/** Three or four of one rank, each from a different suit. */
function isSet(cards: readonly HeldCard[]): boolean {
  if (cards.length > Most) {
    return false;
  }

  const named = cards.filter((card) => !isJoker(card));

  if (named.length === 0) {
    return false;
  }

  const rank = rankOf(named[0]);
  const suits = new Set(named.map(suitOf));

  return named.every((card) => rankOf(card) === rank) && suits.size === named.length;
}

/**
 * Three or more of one suit in order, up or down as they happen to be laid,
 * with a joker standing in for whatever is missing. Q-K-A is a run and so
 * is A-2-3, which is the whole reason the ace is asked about twice.
 */
function isRun(cards: readonly HeldCard[]): boolean {
  const named = cards.filter((card) => !isJoker(card));

  if (named.length === 0) {
    return false;
  }

  const suit = suitOf(named[0]);

  if (!named.every((card) => suitOf(card) === suit)) {
    return false;
  }

  const high = (card: HeldCard) => rankOf(card);
  const low = (card: HeldCard) => (rankOf(card) === Ace ? 1 : rankOf(card));

  return (
    inOrderBy(cards, high, 1, 2, Ace) ||
    inOrderBy(cards, high, -1, 2, Ace) ||
    inOrderBy(cards, low, 1, 1, King) ||
    inOrderBy(cards, low, -1, 1, King)
  );
}

/**
 * Anchored on the first card that names itself, since a joker names
 * nothing and takes whatever rank the place it sits in asks for.
 */
function inOrderBy(
  cards: readonly HeldCard[],
  rank: (card: HeldCard) => number,
  step: number,
  lowest: number,
  highest: number,
): boolean {
  const anchor = cards.findIndex((card) => !isJoker(card));
  const from = rank(cards[anchor]);

  return cards.every((card, at) => {
    const wanted = from + step * (at - anchor);

    if (wanted < lowest || wanted > highest) {
      return false;
    }

    return isJoker(card) || rank(card) === wanted;
  });
}
