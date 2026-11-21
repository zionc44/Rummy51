import { describe, expect, it } from 'vitest';
import { Ace, HeldCard, Jack, King, isJoker, rankOf, suitOf } from './card';

/**
 * How a card names itself, and the way every other suite here names one.
 * The codes are the server's own - two digits, zero padded - because a
 * test that invented its own spelling would hold the twins to each other
 * and to nothing real.
 */
let given = 0;

const card = (code: string): HeldCard => ({ id: (given += 1), code });

const suited = (suit: string) => (rank: number) =>
  card(`${suit}-${String(rank).padStart(2, '0')}`);

export const S = suited('S');
export const H = suited('H');
export const D = suited('D');
export const C = suited('C');

export const joker = (face = 1) => card(`J-${String(face).padStart(2, '0')}`);

describe('a card', () => {
  it('rule 1.2: knows its rank', () => {
    expect(rankOf(S(2))).toBe(2);
    expect(rankOf(H(10))).toBe(10);
    expect(rankOf(C(Ace))).toBe(14);
  });

  it('rule 1.1: knows its suit, and two of a kind are the same card', () => {
    expect(suitOf(C(5))).toBe(0);
    expect(suitOf(D(5))).toBe(1);
    expect(suitOf(H(5))).toBe(2);
    expect(suitOf(S(5))).toBe(3);
    expect(S(5).code).toBe(S(5).code);
  });

  it('rule 1.1: and is told from its twin by its number, not its name', () => {
    expect(S(7).id).not.toBe(S(7).id);
  });

  it('rule 1.3: a joker is neither', () => {
    expect(isJoker(joker())).toBe(true);
    expect(isJoker(joker(2))).toBe(true);
    expect(isJoker(S(Ace))).toBe(false);
  });

  it('rule 1.2: the ace is at the top and the pictures below it', () => {
    expect(Ace).toBe(14);
    expect(King).toBe(13);
    expect(Jack).toBe(11);
  });
});
