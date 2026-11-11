import { Ace, HeldCard, King, isJoker, rankOf, suitOf } from './card';
import { worthOfHand, worthOfRank } from './worth';

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

/** What a first showing has to come to. */
export const GoingDown = 51;

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

/** Whether these cards are a run or a set, which is what a meld is. */
export const isMeld = (cards: readonly HeldCard[]) => isSet(cards) || isRun(cards);

/** Three or four of one rank, each from a different suit. */
export function isSet(cards: readonly HeldCard[]): boolean {
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
export function isRun(cards: readonly HeldCard[]): boolean {
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

/**
 * Whether this meld holds a clean run: three or more in a row with no joker
 * among them. A run with no joker at all is clean end to end; one with a
 * joker holds a clean run only where three of its cards still sit together.
 * 6-7-8-joker-10 holds one and 5-6-joker-8-9 does not.
 */
export const hasCleanRun = (meld: readonly HeldCard[]) => cleanRunIn(meld).length >= Least;

/**
 * What a meld lying on the table is worth. Every card counts for itself,
 * and a joker counts for the card it is standing in for: in a set that is
 * the rank, and in a run it is whatever the place it fills asks for.
 */
export function worthOnTable(meld: readonly HeldCard[]): number {
  if (meld.length < Least || !isMeld(meld)) {
    return 0;
  }

  return isSet(meld)
    ? meld.length * worthOfRank(rankOf(meld.filter((card) => !isJoker(card))[0]))
    : placesOf(meld).reduce((all, place) => all + worthOfRank(place === 1 ? Ace : place), 0);
}

/**
 * What a showing comes to. Every meld counts for what it is worth - except
 * the one run providing the clean three, which counts only its clean part.
 *
 * Which run provides it is whichever costs least to give up, and a run with
 * no joker costs nothing at all: it is clean end to end, so a showing that
 * holds one is never docked. Only a player whose clean three is buried
 * inside a run with a joker pays for it.
 */
export function showingWorth(laid: readonly (readonly HeldCard[])[]): number {
  const whole = laid.reduce((all, meld) => all + worthOnTable(meld), 0);
  const providers = laid
    .filter(hasCleanRun)
    .map((meld) => worthOnTable(meld) - worthOfHand(cleanRunIn(meld)));

  return providers.length === 0 ? whole : whole - Math.min(...providers);
}

/**
 * The rank each place in a run asks for, jokers included, read off
 * whichever way the run is in order. Empty where it is no run.
 */
function placesOf(run: readonly HeldCard[]): readonly number[] {
  const high = (card: HeldCard) => rankOf(card);
  const low = (card: HeldCard) => (rankOf(card) === Ace ? 1 : rankOf(card));

  for (const [rank, step, lowest, highest] of [
    [high, 1, 2, Ace],
    [high, -1, 2, Ace],
    [low, 1, 1, King],
    [low, -1, 1, King],
  ] as const) {
    if (inOrderBy(run, rank, step, lowest, highest)) {
      const anchor = run.findIndex((card) => !isJoker(card));
      const from = rank(run[anchor]);

      return run.map((_, at) => from + step * (at - anchor));
    }
  }

  return [];
}

/** The longest stretch of the meld with no joker in it. */
function cleanRunIn(meld: readonly HeldCard[]): readonly HeldCard[] {
  if (!isRun(meld)) {
    return [];
  }

  let longest: readonly HeldCard[] = [];
  let running: readonly HeldCard[] = [];

  for (const card of meld) {
    running = isJoker(card) ? [] : [...running, card];

    if (running.length > longest.length) {
      longest = running;
    }
  }

  return longest;
}
