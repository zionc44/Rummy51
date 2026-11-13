/** The one place the path to a card is written. CLAUDE.md says which deck. */
const Deck = 'cards/corners/';

export const pictureOf = (code: string) => `${Deck}${code}.svg`;
