/**
 * A card in a hand, and which card it is. Rule 1.1 puts two of every card in
 * the deck, so a code names a picture and not a card: the id is what tells
 * one seven of hearts from the other, and what a hand is rearranged by.
 */
export type MeldEnd = 'front' | 'back';

/** Rule 5.9. */
export interface Fit {
  readonly meld: number;
  readonly end: MeldEnd;
}

/** Rule 5.11. */
export interface Swap {
  readonly meld: number;
  readonly at: number;
}

export interface HeldCard {
  readonly id: number;
  readonly code: string;

  /**
   * Worked out by the server, the rule that decides it living there. Absent
   * on a card already on the table, and on one in a hand nobody may see.
   */
  readonly fits?: readonly Fit[];

  /** The same, for the jokers this card could buy. */
  readonly buys?: readonly Swap[];

  /** Rule 3.4, told only to its owner under 11.1. */
  readonly owed?: boolean;
}

/** As the cards name themselves. */
const Suits = 'CDHS';

export const isJoker = (card: HeldCard) => card.code.startsWith('J-');

/** Rules 1.2 and 4.2: fourteen here, and the ace asked about twice. */
export const rankOf = (card: HeldCard) => Number(card.code.slice(2));

export const suitOf = (card: HeldCard) => Suits.indexOf(card.code[0]);

export const Ace = 14;
export const King = 13;
export const Jack = 11;
