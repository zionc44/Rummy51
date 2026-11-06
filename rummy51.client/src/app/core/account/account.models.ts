export interface ChangeDisplayNameRequest {
  readonly displayName: string;
}

export interface SetPasswordRequest {
  readonly password: string;
}

export interface ChangePasswordRequest {
  readonly currentPassword: string;
  readonly newPassword: string;
}
