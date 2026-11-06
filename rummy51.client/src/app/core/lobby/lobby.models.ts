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
export type GameMode = 'discrete' | 'survival' | 'series';

export const GAME_MODES: readonly GameMode[] = ['discrete', 'survival', 'series'];
