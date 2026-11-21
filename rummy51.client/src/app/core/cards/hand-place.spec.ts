import { describe, expect, it } from 'vitest';
import { Place, placeFor } from './hand-place';

/**
 * Where a card being carried would land. Everything comes in already
 * measured, so none of this needs a screen - which is what the file says
 * about itself and what these hold it to.
 */
const Card = { width: 40, height: 60 };

/** A line of places, left to right, with nulls for gaps kept in the air. */
const line = (ids: readonly (number | null)[], top: number): readonly Place[] =>
  ids.map((id, at) => ({
    id,
    box: new DOMRect(at * Card.width, top, Card.width, Card.height),
  }));

/** The middle of the place at that step, which is where a finger would be. */
const over = (step: number, top: number) => ({
  x: step * Card.width + Card.width / 2,
  y: top + Card.height / 2,
});

describe('a card let go of over one line', () => {
  const one = [line([1, 2, 3, 4], 0)];

  it('lands before everything when it is let go at the start', () => {
    expect(placeFor({ x: -10, y: 30 }, one, 1).to).toBe(0);
  });

  it('lands after whatever it has passed', () => {
    expect(placeFor(over(2, 0), one, 1).to).toBe(2);
  });

  it('lands at the end when it is let go past them all', () => {
    expect(placeFor({ x: 1000, y: 30 }, one, 1).to).toBe(4);
  });

  it('and moves no break, there being no second line to cross into', () => {
    expect(placeFor(over(2, 0), one, 1).firstLine).toBeNull();
  });
});

describe('a card let go of over two lines', () => {
  const two = [line([1, 2, 3], 0), line([4, 5, 6], 100)];

  it('is counted over the whole hand and not over its own line', () => {
    expect(placeFor(over(1, 100), two, 1).to).toBe(3 + 1);
  });

  it('is read on the line it is nearest to', () => {
    expect(placeFor({ x: 10, y: 95 }, two, 1).to).toBe(3);
    expect(placeFor({ x: 10, y: 35 }, two, 1).to).toBe(0);
  });

  it('carried onto the first line, the break grows by what it brought', () => {
    expect(placeFor(over(1, 0), two, 1).firstLine).toBe(4);
    expect(placeFor(over(1, 0), two, 3).firstLine).toBe(6);
  });

  it('and carried onto the second, it is what the first is left holding', () => {
    expect(placeFor(over(1, 100), two, 1).firstLine).toBe(3);
  });
});

describe('places kept for cards still in the air', () => {
  const gapped = [line([1, null, 3], 0)];

  it('are not counted, a card in the air lying nowhere', () => {
    expect(placeFor({ x: 1000, y: 30 }, gapped, 1).to).toBe(2);
  });
});
