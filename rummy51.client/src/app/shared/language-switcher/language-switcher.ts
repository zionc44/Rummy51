import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../core/i18n/language-service';
import { LanguageCode } from '../../core/i18n/languages';

@Component({
  selector: 'app-language-switcher',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center gap-1" role="group" [attr.aria-label]="'language.switch' | translate">
      @for (language of languages.available; track language.code) {
        <button
          type="button"
          (click)="choose(language.code)"
          [attr.aria-current]="language.code === languages.current().code ? 'true' : null"
          class="rounded-full px-3 py-1.5 text-sm font-medium transition
                 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-felt-50"
          [class]="
            language.code === languages.current().code
              ? 'bg-white/15 text-white'
              : 'text-white/60 hover:bg-white/10 hover:text-white'
          "
        >
          {{ language.label }}
        </button>
      }
    </div>
  `,
})
export class LanguageSwitcher {
  protected readonly languages = inject(LanguageService);

  protected choose(code: LanguageCode): void {
    this.languages.use(code);
  }
}
