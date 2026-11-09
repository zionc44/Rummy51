/**
 * Where a card being carried would go if it were let go here.
 *
 * It is asked of the hand as it is laid at this moment - two lines or one,
 * cards and the gaps kept for the ones in the air - and of nothing else. No
 * element is written to and no measurement is taken: the boxes come in
 * already measured, which is what makes this answerable on paper.
 */

/** One place in a line: a card lying there, or a gap kept for one. */
export interface Place {
  readonly id: number | null;
  readonly box: DOMRect;
}

export interface Point {
  readonly x: number;
  readonly y: number;
}

/** A place in the hand counted over the cards that are left lying in it. */
export interface Landing {
  readonly to: number;

  /**
   * How many cards the first line holds once this has landed, or null where
   * the hand is on one line and there is no second to cross into.
   */
  readonly firstLine: number | null;
}

export function placeFor(
  pointer: Point,
  lines: readonly (readonly Place[])[],
  carrying: number,
): Landing {
  const line = lineUnder(pointer.y, lines);
  const counted = countedAt(pointer.x, lines, line);

  return {
    to: line === 0 ? counted.within : counted.inFirst + counted.within,
    // Crossing between the lines is what moves the break, and nothing else
    // does: eight cards above stay eight, and nine stay nine.
    firstLine:
      lines.length < 2 ? null : line === 0 ? counted.inFirst + carrying : counted.inFirst,
  };
}

/**
 * The line the pointer is over, by the first place in each: a line is a
 * straight row, so one place in it says where the whole of it lies.
 */
function lineUnder(y: number, lines: readonly (readonly Place[])[]): number {
  let nearest = Number.POSITIVE_INFINITY;
  let line = 0;

  for (const [index, places] of lines.entries()) {
    const box = places[0]?.box;

    if (box === undefined) {
      continue;
    }

    const away = Math.abs(y - (box.top + box.height / 2));

    if (away < nearest) {
      nearest = away;
      line = index;
    }
  }

  return line;
}

/**
 * How many cards the pointer has passed on its own line, and how many are
 * on the first - which is what turns a place on a line into a place in the
 * hand. Gaps are not counted: a card in the air is not lying anywhere.
 */
function countedAt(
  x: number,
  lines: readonly (readonly Place[])[],
  line: number,
): { within: number; inFirst: number } {
  let within = 0;
  let inFirst = 0;

  for (const [index, places] of lines.entries()) {
    for (const place of places) {
      if (place.id === null) {
        continue;
      }

      if (index === 0) {
        inFirst++;
      }

      if (index === line && place.box.left + place.box.width / 2 < x) {
        within++;
      }
    }
  }

  return { within, inFirst };
}
