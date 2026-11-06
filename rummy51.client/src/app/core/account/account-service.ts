import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthService } from '../auth/auth-service';
import { PlayerSummary } from '../auth/auth.models';
import { failWithApiErrors } from '../errors/api-error';
import { ChangeDisplayNameRequest, ChangePasswordRequest, SetPasswordRequest } from './account.models';

const API = '/api/account';

/** What the signed-in player changes about themselves. */
@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  changeDisplayName(request: ChangeDisplayNameRequest): Observable<PlayerSummary> {
    return this.adopting(this.http.put<PlayerSummary>(`${API}/display-name`, request));
  }

  /** For an account that has none. The server refuses it when one exists. */
  setPassword(request: SetPasswordRequest): Observable<PlayerSummary> {
    return this.adopting(this.http.post<PlayerSummary>(`${API}/password`, request));
  }

  changePassword(request: ChangePasswordRequest): Observable<PlayerSummary> {
    return this.adopting(this.http.put<PlayerSummary>(`${API}/password`, request));
  }

  /**
   * Every change answers with the player as they now are, so the session the
   * rest of the app reads follows from the reply instead of a second request.
   */
  private adopting(change: Observable<PlayerSummary>): Observable<PlayerSummary> {
    return change.pipe(
      tap((player) => this.auth.updateCurrentPlayer(player)),
      failWithApiErrors(),
    );
  }
}
