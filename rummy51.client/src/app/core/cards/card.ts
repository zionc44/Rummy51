/**
 * A card in a hand, and which card it is. The deck is two packs, so a code
 * names a picture and not a card: the id is what tells one seven of hearts
 * from the other, and it is what a hand is rearranged by.
 */
/** Which end of a meld a card joins it at. */
export type MeldEnd = 'front' | 'back';

/** A place a card could join what is already lying on the table. */
export interface Fit {
  readonly meld: number;
  readonly end: MeldEnd;
}

/**
 * A joker lying on the table that a card is standing in for, and so could
 * buy out of the meld it lies in.
 */
export interface Swap {
  readonly meld: number;
  readonly at: number;
}

export interface HeldCard {
  readonly id: number;
  readonly code: string;

  /**
   * Where this card could join a meld on the table, worked out by the
   * server because the rule that decides it lives there. Absent on a card
   * lying on the table, which has nowhere left to go, and on one in a hand
   * nobody may see.
   */
  readonly fits?: readonly Fit[];

  /** The jokers on the table this card is the card for, worked out the same way. */
  readonly buys?: readonly Swap[];
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
export const Jack = 11;
