import { HeldCard } from '../cards/card';

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

/** Rule 9.2. */
export type GameMode = 'single' | 'survival' | 'series';

export const GAME_MODES: readonly GameMode[] = ['single', 'survival', 'series'];

export const TABLE_SEATS: readonly number[] = [2, 3, 4];

/** Rule 1.1. */
export const JOKER_COUNTS: readonly number[] = [0, 2, 4];

/** Rules 8.1 and 9.2. Ceilings, not targets: a
    score climbs with bad play and these are ceilings, not targets. */
export const SURVIVAL_POINTS: readonly number[] = [100, 200, 500];

/**
 * By the number of players: rule 2.1 moves the deal on by one, so a series
 * that did not divide would give somebody an extra deal.
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
 * The seats keep their order: whoever opened the table took the first, and
 * someone leaving empties a seat rather than moving everyone along.
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
export type HandPhase =
  | 'cutting'
  | 'dealing'
  | 'playing'
  | 'offering'
  | 'answered'
  | 'keeping'
  | 'finished';

/** One pass of the deal: how many cards landed at which seat. */
export interface DealPass {
  readonly seat: number;
  readonly count: number;
}

/**
 * Who the table is waiting for, and how much longer it will wait. A state
 * and not an instruction: whoever opens the page halfway through a wait is
 * told what is left of it, so their ring matches everybody else's instead
 * of starting again.
 */
export interface Waiting {
  readonly seat: number;
  /** As of this message, which is what the ring is drawn from. */
  readonly secondsLeft: number;
  /** The whole of the wait, so the arc can be drawn as a fraction of it. */
  readonly seconds: number;
}

/** One run or set, and whose it was. Rules 5.7, 8.1, 11.3. */
export interface MeldView {
  readonly seat: number;
  readonly cards: readonly HeldCard[];
}

/**
 * The hand as everyone at the table may see it, which is chapter 11 and
 * nothing beyond it. Cards arrive as their code, which is also the name of
 * the picture, so there is one name and not two.
 */
export interface HandView {
  readonly phase: HandPhase;
  readonly dealerSeat: number;
  readonly cutterSeat: number;
  /** Rule 11.5. */
  readonly deckSize: number;
  /** Rule 2.2. Null until they have. */
  readonly cutAt: number | null;
  /** Rule 11.5: no secret, and decoration only. */
  readonly backs: readonly string[];
  /** Rule 11.5 says nobody may know. Empty but for a development setting. */
  readonly jokerPositions: readonly number[];
  readonly revealedAtCut: readonly string[];
  readonly finishingCard: string | null;
  /** Rule 11.2. */
  readonly cardsPerSeat: readonly number[];
  /** Rule 11.6. */
  readonly passes: readonly DealPass[];
  readonly stockSize: number;
  /** Whose turn it is, or -1 before play has begun. */
  readonly turnSeat: number;

  /** Rules 6.1, 11.4. -1 while the hand is still being played. */
  readonly winnerSeat: number;

  /** Rules 7.1, 11.4. */
  readonly canTakeFinishing: boolean;

  /** Rules 12.1, 12.2. */
  readonly canOfferRedeal: boolean;

  /** Rule 12.1, or -1 when nothing is on the table to answer. */
  readonly offeredBy: number;

  /** Rule 12.9: the offer on the table cannot simply be refused. */
  readonly forced: boolean;

  /** Rule 12.11, and empty until the table may see them. */
  readonly forcedWith: readonly string[];

  /** Rule 11.3. */
  readonly wentDown: readonly boolean[];

  /** Rule 11.4. */
  readonly hasDrawn: boolean;

  /** Rule 3.3. Null until the hand's first discard. */
  readonly discardTop: string | null;
  readonly discardCount: number;
  readonly melds: readonly MeldView[];
  /** Rule 3.6. */
  readonly canUndo: boolean;

  /** Rule 5.7. */
  readonly canAdd: boolean;
  readonly waiting: Waiting | null;
}

/** Where a player reached. Rules 3.2 and 7.1. */
export type DrawnFrom = 'stock' | 'discards' | 'finishing';

export interface CreateTableRequest {
  readonly seats: number;
  readonly jokers: number;
  readonly mode: GameMode;
  /** Points that end a survival game, or hands in a series. Null for one hand. */
  readonly limit: number | null;
}

/**
 * Chapter 12 as one player may see it, which is the half of it that is not
 * the whole table's. The pairs here are this player's own and say that they
 * may force a redeal with them; everyone else's arrive, if ever, in the
 * hand's own `forcing`.
 */
export interface Redeal {
  /** Rule 12.4, masked by the server: a null is a seat this player may not see. */
  readonly answers: readonly (boolean | null)[];

  /** Rule 12.9, empty where there is no right to force. */
  readonly pairs: readonly string[];
}
