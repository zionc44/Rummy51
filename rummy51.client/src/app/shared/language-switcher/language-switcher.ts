import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../core/i18n/language-service';
import { isLanguageCode } from '../../core/i18n/languages';

/**
 * A native select rather than a custom menu: on a phone it opens the system
 * picker, and keyboard support and screen-reader semantics come for free. The
 * list grows by adding an entry to LANGUAGES, with nothing to change here.
 */
@Component({
  selector: 'app-language-switcher',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative inline-flex items-center">
      <label class="sr-only" for="language-switcher">{{ 'language.switch' | translate }}</label>

      <svg
        class="pointer-events-none absolute start-2.5 size-4 text-white/50"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="1.8"
        aria-hidden="true"
      >
        <circle cx="12" cy="12" r="9" />
        <path d="M3 12h18M12 3c2.5 2.7 2.5 15.3 0 18M12 3c-2.5 2.7-2.5 15.3 0 18" />
      </svg>

      <!-- Each option declares whether it is the selected one. Binding value on
           the select instead would run before @for has created the options,
           leaving the browser to fall back to the first in the list. -->
      <select
        id="language-switcher"
        (change)="choose($event)"
        class="cursor-pointer appearance-none rounded-full bg-white/10 py-1.5 pe-8 ps-9
               text-sm font-medium text-white transition hover:bg-white/15
               focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-felt-50"
      >
        @for (language of languages.available; track language.code) {
          <!-- The option list is drawn by the operating system, which ignores
               the control's colours, so each option sets its own. -->
          <option
            [value]="language.code"
            [selected]="language.code === languages.current().code"
            class="bg-felt-900 text-white"
          >
            {{ language.label }}
          </option>
        }
      </select>

      <svg
        class="pointer-events-none absolute end-2.5 size-4 text-white/50"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        aria-hidden="true"
      >
        <path d="m6 9 6 6 6-6" />
      </svg>
    </div>
  `,
})
export class LanguageSwitcher {
  protected readonly languages = inject(LanguageService);

  protected choose(event: Event): void {
    const code = (event.target as HTMLSelectElement).value;

    if (isLanguageCode(code)) {
      this.languages.use(code);
    }
  }
}
