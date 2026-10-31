import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { AuthService } from './core/auth/auth-service';
import { routes } from './app.routes';
import { DEFAULT_LANGUAGE, FALLBACK_LANGUAGE } from './core/i18n/languages';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // The cookie is httpOnly, so the only way to learn whether a session is
    // live is to ask. Doing it before the first render keeps a returning
    // player from seeing the landing page flash past on the way to the lobby.
    provideAppInitializer(() => inject(AuthService).restoreSession()),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch()),
    provideTranslateService({
      lang: DEFAULT_LANGUAGE,
      fallbackLang: FALLBACK_LANGUAGE,
      loader: provideTranslateHttpLoader({ prefix: 'i18n/', suffix: '.json' }),
    }),
  ],
};
