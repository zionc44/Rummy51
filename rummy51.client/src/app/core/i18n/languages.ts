export interface Language {
  readonly code: LanguageCode;
  readonly label: string;
  readonly dir: 'rtl' | 'ltr';
}

export type LanguageCode = 'he' | 'en';

export const LANGUAGES: readonly Language[] = [
  { code: 'he', label: 'עברית', dir: 'rtl' },
  { code: 'en', label: 'English', dir: 'ltr' },
];

export const DEFAULT_LANGUAGE: LanguageCode = 'he';

/** Used for any key the chosen language is missing. */
export const FALLBACK_LANGUAGE: LanguageCode = 'en';

export const isLanguageCode = (value: unknown): value is LanguageCode =>
  LANGUAGES.some((language) => language.code === value);
