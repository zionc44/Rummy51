import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageSwitcher } from '../language-switcher/language-switcher';

/**
 * The frame every page sits in: the felt background, and a header carrying the
 * name and the language picker. Pages project their own content, and anything
 * extra for the header through the `shell-actions` slot.
 */
@Component({
  selector: 'app-page-shell',
  imports: [RouterLink, TranslatePipe, LanguageSwitcher],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-h-dvh flex-col bg-gradient-to-b from-felt-900 via-felt-950 to-black text-white">
      <header class="flex items-center justify-between px-4 py-4 sm:px-8">
        <a
          routerLink="/"
          class="text-sm font-bold tracking-wide text-white/70 transition hover:text-white"
        >
          {{ 'app.title' | translate }}
        </a>

        <div class="flex items-center gap-3">
          <app-language-switcher />
          <ng-content select="[shell-actions]" />
        </div>
      </header>

      <ng-content />
    </div>
  `,
})
export class PageShell {}
