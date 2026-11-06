export interface PlayerSummary {
  readonly playerId: string;
  readonly email: string;
  readonly displayName: string;
  /** Supplied by whoever signed the player in. Null until pictures can be chosen. */
  readonly avatarUrl: string | null;
  /** False for an account created through a provider, until one is chosen. */
  readonly hasPassword: boolean;
}

export interface RegisterRequest {
  readonly email: string;
  readonly password: string;
  readonly displayName: string;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
  readonly rememberMe: boolean;
}
