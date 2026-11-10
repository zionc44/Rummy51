import { ChangeDetectionStrategy, Component, input } from '@angular/core';
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
  host: { '[class.shell--fits]': 'fits()' },
  templateUrl: './page-shell.html',
  styleUrl: './page-shell.scss',
})
export class PageShell {
  /**
   * Whether this page has to fit the screen it is on rather than run past
   * it. Most do not: a form is read from the top down and scrolling is how
   * that is done. A table is not read, it is played, and a card that is
   * below the fold is a card nobody can reach.
   */
  readonly fits = input(false);
}
