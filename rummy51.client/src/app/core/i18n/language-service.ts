import { DOCUMENT, Injectable, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import {
  DEFAULT_LANGUAGE,
  LANGUAGES,
  Language,
  LanguageCode,
  isLanguageCode,
} from './languages';

const STORAGE_KEY = 'rummy51.language';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly document = inject(DOCUMENT);

  private readonly code = signal<LanguageCode>(DEFAULT_LANGUAGE);

  /**
   * Every code in the list has an entry, so the search always finds one -
   * but that is a fact about the data and the type cannot carry it. Falling
   * back to the first is what a reader would do anyway, and it means this
   * never has to claim to know something it does not.
   */
  readonly current = computed<Language>(
    () => LANGUAGES.find((language) => language.code === this.code()) ?? LANGUAGES[0],
  );

  readonly available = LANGUAGES;

  constructor() {
    this.use(this.remembered() ?? DEFAULT_LANGUAGE);
  }

  use(code: LanguageCode): void {
    this.code.set(code);
    this.translate.use(code);

    // Hebrew reads right to left, so the direction travels with the language.
    // Styling uses logical properties, which follow this attribute.
    const element = this.document.documentElement;
    element.lang = code;
    element.dir = this.current().dir;

    this.remember(code);
  }

  /** The other languages, for a switcher that only offers what is not current. */
  readonly alternatives = computed(() =>
    LANGUAGES.filter((language) => language.code !== this.code()),
  );

  private remembered(): LanguageCode | null {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return isLanguageCode(stored) ? stored : null;
    } catch {
      // Private browsing and blocked site data both throw here. The default
      // language is a fine answer.
      return null;
    }
  }

  private remember(code: LanguageCode): void {
    try {
      localStorage.setItem(STORAGE_KEY, code);
    } catch {
      // A preference that cannot be stored is not worth failing over.
    }
  }
}
