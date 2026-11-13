import { Ace, HeldCard, Jack, isJoker, rankOf } from './card';

/**
 * Rules 1.2 and 1.3, the twin of `Worth` on the server. It is here so the
 * screen can answer at once and it is a copy: the server decides, the two
 * are changed together or not at all, and where they disagree the server
 * is right.
 */

/** Rule 8.1. */
export const NeverWentDown = 100;

const Picture = 10;
const AceWorth = 11;
const JokerInHand = 20;

export const worthOfRank = (rank: number) =>
  rank === Ace ? AceWorth : rank >= Jack ? Picture : rank;

export const worthHeld = (card: HeldCard) =>
  isJoker(card) ? JokerInHand : worthOfRank(rankOf(card));

export const worthOfHand = (cards: readonly HeldCard[]) =>
  cards.reduce((all, card) => all + worthHeld(card), 0);
