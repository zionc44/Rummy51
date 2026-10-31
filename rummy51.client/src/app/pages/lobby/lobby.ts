import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { PageShell } from '../../shared/page-shell/page-shell';

// Placeholder: enough to close the loop after signing in, no more.
@Component({
  selector: 'app-lobby',
  imports: [TranslatePipe, PageShell],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-shell>
      <button
        shell-actions
        type="button"
        (click)="logout()"
        class="rounded-full border border-white/20 px-4 py-1.5 text-sm font-medium
               text-white/80 transition hover:bg-white/10 hover:text-white"
      >
        {{ 'auth.logout' | translate }}
      </button>

      <main class="flex flex-1 flex-col items-center justify-center gap-3 px-6 text-center">
        <h1 class="text-4xl font-black">
          {{ 'lobby.welcome' | translate: { '0': auth.currentPlayer()?.displayName } }}
        </h1>
        <p class="text-white/50">{{ 'lobby.placeholder' | translate }}</p>
      </main>
    </app-page-shell>
  `,
})
export class Lobby {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/']));
  }
}
