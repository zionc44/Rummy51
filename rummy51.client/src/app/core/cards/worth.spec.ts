import { describe, expect, it } from 'vitest';
import { C, D, H, S, joker } from './card.spec';
import { Ace, Jack, King } from './card';
import { NeverWentDown, worthHeld, worthOfHand, worthOfRank } from './worth';

/**
 * The twin of `Worth.cs`. Every answer here is one the server gives too,
 * and the two are changed together or not at all.
 */
describe('what a card is worth', () => {
  it('rule 1.2: a card is worth its number', () => {
    expect(worthHeld(S(2))).toBe(2);
    expect(worthHeld(H(10))).toBe(10);
  });

  it('rule 1.2: every picture is worth ten', () => {
    expect(worthHeld(S(Jack))).toBe(10);
    expect(worthHeld(D(12))).toBe(10);
    expect(worthHeld(C(King))).toBe(10);
  });

  it('rule 1.2: the ace is eleven, wherever it is used', () => {
    expect(worthHeld(S(Ace))).toBe(11);
    expect(worthOfRank(Ace)).toBe(11);
  });

  it('rule 1.3: a joker in a hand is twenty', () => {
    expect(worthHeld(joker())).toBe(20);
  });

  it('rule 8.1: a hand is worth what is in it', () => {
    expect(worthOfHand([joker(), S(Ace), H(King), D(3)])).toBe(20 + 11 + 10 + 3);
  });

  it('rule 8.1: an empty hand is worth nothing', () => {
    expect(worthOfHand([])).toBe(0);
  });

  it('rule 8.1: a hand nobody showed is a hundred whatever is in it', () => {
    expect(NeverWentDown).toBe(100);
  });
});
