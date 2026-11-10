/**
 * Where a card's picture comes from. One deck for the whole game: cards lie
 * over one another nearly everywhere they are shown - in a hand, in a meld,
 * in the fan laid out for the cut - so nearly everywhere they are read by
 * the corner alone, and a deck drawn for that is the deck to read them all
 * from. The few places a card is seen whole lose nothing by it.
 *
 * `public/cards` still holds the deck this one is generated from. Nothing
 * on screen reads it any more.
 */
const Deck = 'cards/corners/';

export const pictureOf = (code: string) => `${Deck}${code}.svg`;
