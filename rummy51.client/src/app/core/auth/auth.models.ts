/** Mirrors AvatarSource on the server, which sends the name rather than the ordinal. */
export type AvatarSource = 'provider' | 'uploaded';

export interface PlayerSummary {
  readonly playerId: string;
  readonly email: string;
  readonly displayName: string;
  /** The picture to show. The server has already chosen between the two below. */
  readonly avatarUrl: string | null;
  /** From whoever signed the player in, refreshed on every sign-in. */
  readonly providerAvatarUrl: string | null;
  /** The one the player uploaded, if there is one. */
  readonly uploadedAvatarUrl: string | null;
  readonly avatarSource: AvatarSource;
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
