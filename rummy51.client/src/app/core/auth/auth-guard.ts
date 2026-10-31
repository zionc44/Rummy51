import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth-service';

/** Keeps a signed-out visitor out of the game. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);

  return auth.isSignedIn() || inject(Router).createUrlTree(['/login']);
};

/** Keeps a signed-in player out of the sign-up and sign-in forms. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);

  return !auth.isSignedIn() || inject(Router).createUrlTree(['/lobby']);
};
