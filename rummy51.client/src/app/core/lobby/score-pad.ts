import { Standing, TableSeat } from './lobby.models';

/** One player's column, and what the game has made true of them. */
export interface PadColumn {
  readonly seat: number;

  /** Empty for a chair given up under 9.6, which the server stops naming. */
  readonly name: string;
  readonly mine: boolean;
  readonly out: boolean;

  /** Rule 9.5, and nought for a chair that was never in the game. */
  readonly place: number;

  /** Rules 9.3 and 10.8: null until that seat has said. */
  readonly staying: boolean | null;
}

/** One player in one hand. */
export interface PadCell {
  readonly score: number;

  /** Rule 9.4's number as it stood after that hand, which is what it is read against. */
  readonly running: number;
}

/** One hand, a cell to a column and null where that seat was not in it. */
export interface PadRow {
  readonly hand: number;
  readonly cells: readonly (PadCell | null)[];
}

export interface ScorePad {
  readonly columns: readonly PadColumn[];
  readonly rows: readonly PadRow[];
}

/**
 * Rule 9.2 laid out as it is played: a column to a player and a row to a
 * hand. The columns keep their seat order rather than their placing, so
 * that a column is the same column from one hand to the next.
 */
export function scorePadOf(
  standing: Standing,
  seats: readonly TableSeat[],
  ownSeat: number,
  staying: readonly (boolean | null)[],
): ScorePad {
  const columns = seats.flatMap((who, seat) =>
    who.playerId === null && (standing.hands[seat] ?? 0) === 0
      ? []
      : [
          {
            seat,
            name: who.displayName ?? '',
            mine: seat === ownSeat,
            out: standing.out[seat] ?? false,
            place: standing.places[seat] ?? 0,
            staying: staying[seat] ?? null,
          },
        ],
  );

  const running = columns.map(() => 0);

  const rows = standing.eachHand.map((hand, played) => ({
    hand: played + 1,
    cells: columns.map((column, at) => {
      const score = hand[column.seat];

      if (score === null || score === undefined) {
        return null;
      }

      running[at] += score;

      return { score, running: running[at] };
    }),
  }));

  return { columns, rows };
}
