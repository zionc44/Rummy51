/**
 * Mirrors PlayerStatus on the server, which sends the name rather than the
 * ordinal. Only `available` is reachable today — the rest wait for games and
 * for an idle rule.
 */
export type PlayerStatus = 'available' | 'idle' | 'inGame' | 'inGameIdle';

export interface LobbyPlayer {
  readonly playerId: string;
  readonly displayName: string;
  readonly avatarUrl: string | null;
  readonly status: PlayerStatus;
}

/** A table is created for one of these and never changes to another. */
export type GameMode = 'single' | 'survival' | 'series';

export const GAME_MODES: readonly GameMode[] = ['single', 'survival', 'series'];

export const TABLE_SEATS: readonly number[] = [2, 3, 4];

/** What ends a survival game. The winner of a hand takes minus thirty, so a
    score climbs with bad play and these are ceilings, not targets. */
export const SURVIVAL_POINTS: readonly number[] = [100, 200, 500];

/**
 * How many hands a series runs for, by the number of players. Every option
 * divides by that number, because each player has to deal the same number of
 * times - which is why this cannot be one list.
 */
export const SERIES_HANDS: Readonly<Record<number, readonly number[]>> = {
  2: [4, 6, 12],
  3: [3, 6, 12],
  4: [4, 8, 12],
};

export interface TableSeat {
  /** Null while the seat is empty, which is the only thing that marks it so. */
  readonly playerId: string | null;
  readonly displayName: string | null;
  readonly avatarUrl: string | null;
}

/**
 * A table as the lobby sees it. The seats keep their order: whoever opened the
 * table took the first one, and someone leaving empties a seat rather than
 * moving everyone along.
 */
export interface LobbyTable {
  readonly tableId: string;
  readonly mode: GameMode;
  /** Points that end a survival game, or hands in a series. Null for one hand. */
  readonly limit: number | null;
  readonly seats: readonly TableSeat[];
  readonly started: boolean;
}

export interface CreateTableRequest {
  readonly seats: number;
  readonly mode: GameMode;
  /** Points that end a survival game, or hands in a series. Null for one hand. */
  readonly limit: number | null;
}
