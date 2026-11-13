import { Ace, HeldCard, King, isJoker, rankOf, suitOf } from './card';
import { worthOfHand, worthOfRank } from './worth';

/**
 * Chapters 4 and 5, the twin of `Melding.cs`. It is here so the screen can
 * band a run under the finger that drew it, and it is a copy: the server
 * decides, the two are changed together or not at all.
 *
 * It reads the hand as it is laid rather than searching it for the best
 * arrangement there could be. A card belongs to a run because the player
 * has put it beside the others, which is why arranging a hand by hand came
 * first - and why one broken across two lines is not one.
 */

/** Rule 4.1. */
const Least = 3;

/** Rule 5.1. */
export const GoingDown = 51;

/** Rule 4.1. */
const Most = 4;

/**
 * Each place given the number of the run it belongs to, or null for none.
 * The numbers only have to differ between neighbours, which is what lets
 * one be told from the next.
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

/** The longest, because a run of five is one band and not two threes. */
function longestFrom(line: readonly (HeldCard | null)[], at: number): number {
  for (let length = line.length - at; length >= Least; length -= 1) {
    const group = line.slice(at, at + length);

    if (group.every((card) => card !== null) && isMeld(group as HeldCard[])) {
      return length;
    }
  }

  return 0;
}

/** Rule 4.1. */
export const isMeld = (cards: readonly HeldCard[]) => isSet(cards) || isRun(cards);

/** Rule 4.1. */
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

/** Rules 4.1, 4.2 - which is why the ace is asked about twice. */
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

/** Rules 5.1, 5.2. */
export const hasCleanRun = (meld: readonly HeldCard[]) => cleanRunIn(meld).length >= Least;

/** Rules 1.3, 5.3. */
export function worthOnTable(meld: readonly HeldCard[]): number {
  if (meld.length < Least || !isMeld(meld)) {
    return 0;
  }

  return isSet(meld)
    ? meld.length * worthOfRank(rankOf(meld.filter((card) => !isJoker(card))[0]))
    : placesOf(meld).reduce((all, place) => all + worthOfRank(place === 1 ? Ace : place), 0);
}

/**
 * Rules 5.3, 5.4. Which run provides the clean three is whichever costs
 * least to give up, and one with no joker costs nothing at all.
 */
export function showingWorth(laid: readonly (readonly HeldCard[])[]): number {
  const whole = laid.reduce((all, meld) => all + worthOnTable(meld), 0);
  const providers = laid
    .filter(hasCleanRun)
    .map((meld) => worthOnTable(meld) - worthOfHand(cleanRunIn(meld)));

  return providers.length === 0 ? whole : whole - Math.min(...providers);
}

/** Jokers included, read off whichever way the run is in order. */
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

/** Rule 5.2. */
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
