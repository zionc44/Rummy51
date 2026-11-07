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

/** Even numbers only: a deck is made of whole packs, and each brings its pair. */
export const JOKER_COUNTS: readonly number[] = [0, 2, 4];

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
  readonly jokers: number;
  readonly seats: readonly TableSeat[];
  readonly started: boolean;
}

/** Mirrors HandPhase on the server, which sends the name rather than the ordinal. */
export type HandPhase = 'cutting' | 'dealing' | 'playing';

/** One pass of the deal: how many cards landed at which seat. */
export interface DealPass {
  readonly seat: number;
  readonly count: number;
}

/**
 * The hand as everyone at the table may see it. Cards arrive as their code,
 * which is also the name of the picture, so there is one name and not two.
 */
export interface HandView {
  readonly phase: HandPhase;
  readonly dealerSeat: number;
  readonly cutterSeat: number;
  /** Face down, so the count is all anyone is told. */
  readonly deckSize: number;
  /** Where the cutter chose, as a position in the deck. Null until they have. */
  readonly cutAt: number | null;
  /** The back each of those cards wears, by position. Decoration only. */
  readonly backs: readonly string[];
  /**
   * Where the jokers are lying in the deck, marked in the fan so a cut onto
   * one can be aimed at. Empty unless the server was asked for them, which
   * only a development setting does.
   */
  readonly jokerPositions: readonly number[];
  readonly revealedAtCut: readonly string[];
  readonly finishingCard: string | null;
  /** How many cards each seat holds. Anybody at a table can count them. */
  readonly cardsPerSeat: readonly number[];
  /** The deal pass by pass. It says how many went where and never which. */
  readonly passes: readonly DealPass[];
  readonly stockSize: number;
}

export interface CreateTableRequest {
  readonly seats: number;
  readonly jokers: number;
  readonly mode: GameMode;
  /** Points that end a survival game, or hands in a series. Null for one hand. */
  readonly limit: number | null;
}
