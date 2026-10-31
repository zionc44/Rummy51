import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, firstValueFrom, map, of, tap, throwError } from 'rxjs';
import { ApiError, apiErrorsOf } from '../errors/api-error';
import { LoginRequest, PlayerSummary, RegisterRequest } from './auth.models';

const API = '/api/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly player = signal<PlayerSummary | null>(null);

  readonly currentPlayer = this.player.asReadonly();
  readonly isSignedIn = computed(() => this.player() !== null);

  /**
   * Asks the server who is signed in. The cookie is httpOnly and encrypted, so
   * this is the only way the client can find out; it runs once at startup.
   */
  async restoreSession(): Promise<void> {
    const player = await firstValueFrom(
      this.http.get<PlayerSummary>(`${API}/me`).pipe(
        // 401 is the ordinary answer for "nobody is signed in", not a fault.
        catchError(() => of(null)),
      ),
    );

    this.player.set(player);
  }

  register(request: RegisterRequest): Observable<PlayerSummary> {
    // Registering does not sign the player in; they continue to the login form.
    return this.http.post<PlayerSummary>(`${API}/register`, request).pipe(failWithApiErrors());
  }

  login(request: LoginRequest): Observable<PlayerSummary> {
    return this.http.post<PlayerSummary>(`${API}/login`, request).pipe(
      tap((player) => this.player.set(player)),
      failWithApiErrors(),
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${API}/logout`, {}).pipe(
      // Whatever the server says, this browser is done with the session.
      catchError(() => of(undefined)),
      tap(() => this.player.set(null)),
      map(() => undefined),
    );
  }
}

/** Replaces the transport-level error with the codes the server sent. */
const failWithApiErrors = <T>() =>
  catchError<T, Observable<never>>((response: HttpErrorResponse) =>
    throwError(() => apiErrorsOf(response) as ApiError[]),
  );
