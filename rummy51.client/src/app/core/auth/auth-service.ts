import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, firstValueFrom, map, of, tap } from 'rxjs';
import { failWithApiErrors } from '../errors/api-error';
import { LoginRequest, PlayerSummary, RegisterRequest } from './auth.models';

const API = '/api/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly player = signal<PlayerSummary | null>(null);

  readonly currentPlayer = this.player.asReadonly();
  readonly isSignedIn = computed(() => this.player() !== null);

  /** Adopts the player as an account endpoint has just rewritten them. */
  updateCurrentPlayer(player: PlayerSummary): void {
    this.player.set(player);
  }

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

  /**
   * A full navigation, not a request: the browser has to visit Google and come
   * back, which it cannot do inside an XHR.
   */
  startGoogleSignIn(): void {
    window.location.href = `${API}/google/start`;
  }

  /** Confirms ownership of the account that already holds the Google address. */
  linkGoogle(password: string): Observable<PlayerSummary> {
    return this.http.post<PlayerSummary>(`${API}/google/link`, { password }).pipe(
      tap((player) => this.player.set(player)),
      failWithApiErrors(),
    );
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
