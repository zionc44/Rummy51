import { describe, expect, it } from 'vitest';
import { Ace, HeldCard, King } from './card';
import { C, D, H, S, joker } from './card.spec';
import { arrangedLike, inOrder, keepingTheirPlaces, moved } from './card-order';

const codes = (cards: readonly HeldCard[]) => cards.map((card) => card.code);

/**
 * The hand as its owner has arranged it. None of this crosses the wire -
 * the server sends a hand and is never told an order - so these are the
 * only rules about it there are.
 */
describe('sorting a hand', () => {
  it('by rank, lowest on the left, which is how a hand reads', () => {
    const sorted = inOrder([S(9), H(2), D(King)], 'byRank');

    expect(codes(sorted)).toEqual([H(2).code, S(9).code, D(King).code]);
  });

  it('by suit, and by rank inside each one', () => {
    const sorted = inOrder([S(5), C(9), C(4)], 'bySuit');

    expect(codes(sorted)).toEqual([C(4).code, C(9).code, S(5).code]);
  });

  it('rule 4.2: the ace sorts beside the king, and opens a run as well', () => {
    const sorted = inOrder([H(Ace), H(2), H(King)], 'byRank');

    expect(codes(sorted)).toEqual([H(2).code, H(King).code, H(Ace).code]);
  });

  it('rule 1.3: a joker is neither rank nor suit, so it goes past the highest', () => {
    const sorted = inOrder([joker(), S(Ace), H(2)], 'byRank');

    expect(sorted[2].code).toBe(joker().code);
  });

  it('and two jokers keep whatever order they had', () => {
    const one = joker(1);
    const other = joker(2);

    expect(inOrder([one, other], 'byRank').map((card) => card.id)).toEqual([one.id, other.id]);
  });

  it('leaves the hand it was given alone', () => {
    const hand = [S(9), H(2)];

    inOrder(hand, 'byRank');

    expect(codes(hand)).toEqual([S(9).code, H(2).code]);
  });
});

describe('carrying cards somewhere else in the hand', () => {
  const hand = [S(2), S(3), S(4), S(5)];

  it('one card, to a place counted over what is left without it', () => {
    expect(codes(moved(hand, [hand[0].id], 2))).toEqual(
      codes([S(3), S(4), S(2), S(5)]),
    );
  });

  it('a whole run at once, and it arrives together', () => {
    expect(codes(moved(hand, [hand[0].id, hand[1].id], 2))).toEqual(
      codes([S(4), S(5), S(2), S(3)]),
    );
  });

  it('and a card that is not in the hand moves nothing', () => {
    expect(moved(hand, [999], 1)).toBe(hand);
  });
});

describe('a hand held against what the server sends', () => {
  it('with nothing arranged yet, it is the hand as it arrived', () => {
    const held = [S(9), H(2)];

    expect(arrangedLike(held, null)).toBe(held);
  });

  it('keeps the places their owner gave them', () => {
    const [nine, two] = [S(9), H(2)];

    expect(codes(arrangedLike([nine, two], [two, nine]))).toEqual([two.code, nine.code]);
  });

  it('a card that has just reached them joins the end', () => {
    const [nine, two, drawn] = [S(9), H(2), D(King)];

    expect(codes(arrangedLike([nine, two, drawn], [two, nine]))).toEqual(
      [two.code, nine.code, drawn.code],
    );
  });

  it('and one that has left takes its place with it', () => {
    const [nine, two] = [S(9), H(2)];

    expect(codes(arrangedLike([nine], [two, nine]))).toEqual([nine.code]);
  });
});

describe('cards that are somewhere else for a while', () => {
  it('rule 3.6: one laid on the table comes back where it was lying', () => {
    const [four, five, six] = [S(4), S(5), S(6)];
    const order = [four, six];

    expect(codes(keepingTheirPlaces(order, [four, five, six]))).toEqual(
      codes([four, five, six]),
    );
  });

  it('cards that left together come back together', () => {
    const [four, five, six, seven] = [S(4), S(5), S(6), S(7)];

    expect(codes(keepingTheirPlaces([four, seven], [four, five, six, seven]))).toEqual(
      codes([four, five, six, seven]),
    );
  });

  it('one that was lying first comes back first', () => {
    const [four, five] = [S(4), S(5)];

    expect(codes(keepingTheirPlaces([five], [four, five]))).toEqual(codes([four, five]));
  });

  it('and with nothing away, the order is handed straight back', () => {
    const order = [S(4), S(5)];

    expect(keepingTheirPlaces(order, order)).toBe(order);
  });
});
