import { Ace, HeldCard, isJoker, rankOf, suitOf } from './card';
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
export const Least = 3;

/** Rule 5.1. */
export const GoingDown = 51;

/** Rule 4.1. */
const Most = 4;

/** Rule 4.2. */
const Lowest = 1;

/** Rule 4.2, and where Ace already sits. */
const Highest = Ace;

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

/** Rule 4.1, the length included: two of a suit in order are not a run yet. */
export const isMeld = (cards: readonly HeldCard[]) =>
  cards.length >= Least && (isSet(cards) || isRun(cards));

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

/** Rules 4.1, 4.2, 4.3. */
export function isRun(cards: readonly HeldCard[]): boolean {
  const named = cards.filter((card) => !isJoker(card));

  if (named.length === 0) {
    return false;
  }

  const suit = suitOf(named[0]);

  if (!named.every((card) => suitOf(card) === suit)) {
    return false;
  }

  return placesOf(cards).length > 0;
}

/** A joker names nothing, so only the rest are asked anything. */
function inOrder(run: readonly HeldCard[], places: readonly number[]): boolean {
  return (
    places.every((place) => place >= Lowest && place <= Highest) &&
    run.every((card, at) => isJoker(card) || sits(card, places[at]))
  );
}

/** Rule 4.2: the ace answers for either end of the scale. */
const sits = (card: HeldCard, place: number) =>
  rankOf(card) === place || (rankOf(card) === Ace && place === Lowest);

/** Rule 4.3. */
const startsAt = (named: HeldCard) => (rankOf(named) === Ace ? [Highest, Lowest] : [rankOf(named)]);

/** Rules 5.1, 5.2. */
export const hasCleanRun = (meld: readonly HeldCard[]) => cleanRunIn(meld).length >= Least;

/** Rules 1.3, 5.3, 5.5. */
export function worthOnTable(meld: readonly HeldCard[]): number {
  if (!isMeld(meld)) {
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

/**
 * Rules 4.1, 4.2, 4.3: the places a run fills, jokers included, and empty
 * for anything that is not one. There is one scale and the ace answers at
 * both ends of it, so nothing has to be told about corners - fifteen is
 * off the scale wherever it is asked for.
 */
function placesOf(run: readonly HeldCard[]): readonly number[] {
  const anchor = run.findIndex((card) => !isJoker(card));

  if (anchor < 0) {
    return [];
  }

  // High before low, so an ace with nothing but jokers either side of it
  // is read at the top of the scale and not the bottom.
  for (const from of startsAt(run[anchor])) {
    for (const step of [1, -1]) {
      const places = run.map((_, at) => from + step * (at - anchor));

      if (inOrder(run, places)) {
        return places;
      }
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
