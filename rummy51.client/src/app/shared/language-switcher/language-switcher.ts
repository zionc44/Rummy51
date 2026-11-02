import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../core/i18n/language-service';
import { isLanguageCode } from '../../core/i18n/languages';
import { IconChevronDown } from '../icons/icon-chevron-down/icon-chevron-down';
import { IconGlobe } from '../icons/icon-globe/icon-globe';

/**
 * A native select rather than a custom menu: on a phone it opens the system
 * picker, and keyboard support and screen-reader semantics come for free. The
 * list grows by adding an entry to LANGUAGES, with nothing to change here.
 */
@Component({
  selector: 'app-language-switcher',
  imports: [TranslatePipe, IconGlobe, IconChevronDown],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './language-switcher.html',
  styleUrl: './language-switcher.scss',
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
