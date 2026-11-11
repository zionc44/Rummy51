import { Ace, HeldCard, Jack, isJoker, rankOf } from './card';

/**
 * What a card counts for, which is the same table `Worth` holds on the
 * server. This one is here so the screen can answer the player at once, and
 * it is a copy: the server decides, and what it decides is what happens.
 * The two are changed together or not at all.
 *
 * A joker is two questions. In a hand it is twenty - a card nobody got to
 * use, and the most expensive thing to be caught holding. On the table it
 * is worth the card it is standing in for, because there it is that card.
 */

/** What a hand costs while its owner has never gone down, whatever it holds. */
export const NeverWentDown = 100;

const Picture = 10;
const AceWorth = 11;
const JokerInHand = 20;

/** The ace eleven, every picture ten, every number itself. */
export const worthOfRank = (rank: number) =>
  rank === Ace ? AceWorth : rank >= Jack ? Picture : rank;

/** A card held in a hand, where a joker is worth twenty. */
export const worthHeld = (card: HeldCard) =>
  isJoker(card) ? JokerInHand : worthOfRank(rankOf(card));

export const worthOfHand = (cards: readonly HeldCard[]) =>
  cards.reduce((all, card) => all + worthHeld(card), 0);
