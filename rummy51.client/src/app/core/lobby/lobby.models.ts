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

/**
 * Every stake a mode's tables are headed by in the lobby, lowest first. A
 * single hand has none, there being nothing for it to reach. A series has
 * every count any table could have been opened for, whoever sits at it.
 */
export function limitsOf(mode: GameMode): readonly number[] {
  switch (mode) {
    case 'survival':
      return SURVIVAL_POINTS;
    case 'series':
      return [...new Set(Object.values(SERIES_HANDS).flat())].sort((one, other) => one - other);
    default:
      return [];
  }
}

export interface TableSeat {
  /** Null while the seat is empty, which is the only thing that marks it so. */
  readonly playerId: string | null;
  readonly displayName: string | null;
  readonly avatarUrl: string | null;

  /** Rule 10.1: holding the seat without watching the table. */
  readonly away: boolean;
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
  | 'restocking'
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
/** Rules 8.2 and 8.4 as they were arrived at. All ones while a hand is being played. */
export interface Doubling {
  readonly forTheSeries: number;
  readonly forTheRedeal: number;
  readonly forTheWin: number;
  readonly altogether: number;

  /** Rule 6.6, which is also what makes the win an uncommon one to say so. */
  readonly rummy: boolean;

  /** Rule 7.1. */
  readonly finishingCard: boolean;
}

/** Rules 9.4 and 9.5: where the game stands, by seat. */
export interface Standing {
  readonly totals: readonly number[];
  readonly hands: readonly number[];

  /**
   * Rule 9.2: what every hand of this game came to, a row to a hand and a
   * seat to a column. Null where a seat was not in that hand, a nought
   * being a hand played and scored nothing in.
   */
  readonly eachHand: readonly (readonly (number | null)[])[];

  readonly out: readonly boolean[];

  /** Who has read what the hand came to, which is what shuts their window. */
  readonly read: readonly boolean[];

  /** Rule 9.5, and nought for a chair that was never in the game. */
  readonly places: readonly number[];

  /** Rules 9.5 and 9.8: the seats the game went to, and none until there is one. */
  readonly won: readonly number[];

  /** Rules 8.4 and 8.5: every hand of a series, and empty for any other game. */
  readonly weights: readonly HandWeight[];
}

/** What a series has made of one of its hands. */
export interface HandWeight {
  /** Rule 8.4. */
  readonly fixed: number;

  /** Rule 8.5. */
  readonly redealt: boolean;
  readonly times: number;
}

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

  /** Rule 9.4: which seats this hand was dealt to. */
  readonly playing: readonly boolean[];
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

  /** Rule 3.5: who has said they have seen the stock filled from the pile. */
  readonly seenTheRestock: readonly boolean[];

  /** Rules 8.1 and 8.2, by seat, and empty until the hand is over. */
  readonly scores: readonly number[];

  /** Rule 8.3, by seat. Empty alongside empty scores. */
  readonly places: readonly number[];

  /** Rule 8.2, broken out so a score can say why it is what it is. */
  readonly doubling: Doubling;

  /** Rules 9.4 and 9.5: the game across its hands, by seat. */
  readonly standing: Standing;

  /** Rules 9.3 and 10.8. Empty while nobody is being asked. */
  readonly staying: readonly (boolean | null)[];

  /** Chapter 10: holding a chair without watching it. */
  readonly away: readonly boolean[];

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
