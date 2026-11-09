/**
 * A card in a hand, and which card it is. The deck is two packs, so a code
 * names a picture and not a card: the id is what tells one seven of hearts
 * from the other, and it is what a hand is rearranged by.
 */
export interface HeldCard {
  readonly id: number;
  readonly code: string;
}

/** Clubs, diamonds, hearts, spades, as the cards name themselves. */
const Suits = 'CDHS';

export const isJoker = (card: HeldCard) => card.code.startsWith('J-');

/**
 * The ace counts fourteen here as everywhere else, because it is worth
 * eleven wherever it is used. A-2-3 is still a run, and whoever asks that
 * question has to ask it of the ace itself.
 */
export const rankOf = (card: HeldCard) => Number(card.code.slice(2));

export const suitOf = (card: HeldCard) => Suits.indexOf(card.code[0]);

export const Ace = 14;
export const King = 13;
