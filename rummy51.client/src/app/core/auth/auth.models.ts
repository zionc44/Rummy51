export interface PlayerSummary {
  readonly playerId: string;
  readonly email: string;
  readonly displayName: string;
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
