import { describe, expect, it } from 'vitest';
import { HandView } from '../lobby/lobby.models';
import { finishingPosition, jokersAtCut, revealedAt, revealedOrder } from './the-cut';

/**
 * Rule 2.2 read off the hand the server sent. Only four of a hand's fields
 * reach any of this, so a whole one would be thirty fields of noise around
 * the four that matter.
 */
const cut = (at: number | null, jokers: readonly string[], finishing: string | null) =>
  ({
    cutAt: at,
    revealedAtCut: jokers,
    finishingCard: finishing,
    deckSize: 108,
  }) as HandView;

describe('what the cut turned up', () => {
  it('nothing before anybody has cut', () => {
    expect(revealedOrder(cut(null, [], null))).toEqual([]);
    expect(revealedOrder(null)).toEqual([]);
    expect(finishingPosition(cut(null, [], null))).toBeNull();
  });

  it('rule 2.2: the card at the cut is the finishing card', () => {
    expect(revealedOrder(cut(40, [], 'S-07'))).toEqual([40]);
    expect(finishingPosition(cut(40, [], 'S-07'))).toBe(40);
  });

  it('rule 2.3: a joker there is the cutter\u2019s, and the card under it finishes', () => {
    expect(revealedOrder(cut(40, ['J-01'], 'S-07'))).toEqual([40, 41]);
    expect(jokersAtCut(cut(40, ['J-01'], 'S-07'))).toEqual([40]);
    expect(finishingPosition(cut(40, ['J-01'], 'S-07'))).toBe(41);
  });

  it('rule 2.3: and so is the one under that, for as many as there are', () => {
    expect(revealedOrder(cut(40, ['J-01', 'J-02'], 'S-07'))).toEqual([40, 41, 42]);
    expect(jokersAtCut(cut(40, ['J-01', 'J-02'], 'S-07'))).toEqual([40, 41]);
  });

  it('rule 2.2: and it is a ring, so it wraps past the end of the deck', () => {
    expect(revealedOrder(cut(107, ['J-01'], 'S-07'))).toEqual([107, 0]);
    expect(finishingPosition(cut(107, ['J-01'], 'S-07'))).toBe(0);
    expect(finishingPosition(cut(107, ['J-01', 'J-02'], 'S-07'))).toBe(1);
  });
});

describe('the cards by where they are lying', () => {
  it('is how they are drawn: a position and what is face up there', () => {
    const laid = revealedAt(cut(40, ['J-01', 'J-02'], 'S-07'));

    expect([...laid]).toEqual([
      [40, 'J-01'],
      [41, 'J-02'],
      [42, 'S-07'],
    ]);
  });

  it('and nothing at all before the cut', () => {
    expect(revealedAt(cut(null, [], null)).size).toBe(0);
  });
});
