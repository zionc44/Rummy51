import { describe, expect, it } from 'vitest';
import { Standing, TableSeat } from './lobby.models';
import { scorePadOf } from './score-pad';

/**
 * Rule 9.2 laid out. Nothing here scores anything - the numbers come from
 * the server already scored - so what is asked is where each of them goes
 * and what the column under it comes to.
 */
describe('a score pad', () => {
  it('rule 9.2: a column to a player and a row to a hand', () => {
    const pad = padOf([
      [-30, 47],
      [22, -30],
    ]);

    expect(pad.columns.map((column) => column.name)).toEqual(['Dana', 'Yossi']);
    expect(pad.rows.map((row) => row.hand)).toEqual([1, 2]);
  });

  it('rule 9.4: and the running total is the column down to that cell', () => {
    const pad = padOf([
      [-30, 47],
      [22, -30],
      [31, 18],
    ]);

    expect(pad.rows.map((row) => row.cells[0]?.running)).toEqual([-30, -8, 23]);
    expect(pad.rows.map((row) => row.cells[1]?.running)).toEqual([47, 17, 35]);
  });

  it('rule 9.4: a seat that was not in a hand has no cell in it', () => {
    const pad = padOf([
      [-30, 47],
      [22, null],
    ]);

    expect(pad.rows[1].cells[1]).toBeNull();
  });

  /** A nought would read as a hand they played and came to nothing in. */
  it('and a hand they missed does not move what they stand at', () => {
    const pad = padOf([
      [-30, 47],
      [22, null],
      [31, null],
    ]);

    expect(pad.rows.map((row) => row.cells[1]?.running ?? null)).toEqual([47, null, null]);
  });

  /**
   * Rule 9.5 places them and the placing moves between hands. A column
   * that moved with it would be a different column every time the pad is
   * read, which is no way to follow one player down it.
   */
  it('rule 9.5: the columns keep their seat order and not their placing', () => {
    const pad = padOf([[80, -30]], { places: [2, 1] });

    expect(pad.columns.map((column) => column.seat)).toEqual([0, 1]);
  });

  it('a chair that was never in the game gets no column', () => {
    const pad = scorePadOf(
      standing([[-30, 47, null]], {}),
      [seated('Dana'), seated('Yossi'), empty()],
      0,
      [],
    );

    expect(pad.columns).toHaveLength(2);
  });

  /** Rule 9.6: the chair empties and what they played is still the game's. */
  it('but one given up after being knocked out keeps it', () => {
    const pad = scorePadOf(
      standing(
        [
          [-30, 47],
          [22, null],
        ],
        { hands: [2, 1] },
      ),
      [seated('Dana'), empty()],
      0,
      [],
    );

    expect(pad.columns).toHaveLength(2);
    expect(pad.rows[0].cells[1]?.score).toBe(47);
  });
});

function padOf(hands: readonly (readonly (number | null)[])[], over: Partial<Standing> = {}) {
  return scorePadOf(standing(hands, over), [seated('Dana'), seated('Yossi')], 0, []);
}

function standing(
  eachHand: readonly (readonly (number | null)[])[],
  over: Partial<Standing>,
): Standing {
  return {
    totals: [],
    hands: [eachHand.length, eachHand.length],
    eachHand,
    out: [],
    read: [],
    places: [],
    won: -1,
    ...over,
  };
}

const seated = (displayName: string): TableSeat => ({
  playerId: displayName,
  displayName,
  avatarUrl: null,
  away: false,
});

const empty = (): TableSeat => ({
  playerId: null,
  displayName: null,
  avatarUrl: null,
  away: false,
});
