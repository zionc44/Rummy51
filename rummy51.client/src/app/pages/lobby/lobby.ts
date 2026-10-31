import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { LanguageSwitcher } from '../../shared/language-switcher/language-switcher';

// Placeholder: enough to close the loop after signing in, no more.
@Component({
  selector: 'app-lobby',
  imports: [TranslatePipe, LanguageSwitcher],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-h-dvh flex-col bg-gradient-to-b from-felt-900 via-felt-950 to-black text-white">
      <header class="flex items-center justify-between px-4 py-4 sm:px-8">
        <span class="text-sm font-bold tracking-wide text-white/70">{{ 'app.title' | translate }}</span>
        <div class="flex items-center gap-3">
          <app-language-switcher />
          <button
            type="button"
            (click)="logout()"
            class="rounded-full border border-white/20 px-4 py-1.5 text-sm font-medium
                   text-white/80 transition hover:bg-white/10 hover:text-white"
          >
            {{ 'auth.logout' | translate }}
          </button>
        </div>
      </header>

      <main class="flex flex-1 flex-col items-center justify-center gap-3 px-6 text-center">
        <h1 class="text-4xl font-black">
          {{ 'lobby.welcome' | translate: { '0': auth.currentPlayer()?.displayName } }}
        </h1>
        <p class="text-white/50">{{ 'lobby.placeholder' | translate }}</p>
      </main>
    </div>
  `,
})
export class Lobby {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/']));
  }
}
