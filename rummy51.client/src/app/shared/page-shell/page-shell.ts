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
  templateUrl: './page-shell.html',
  styleUrl: './page-shell.scss',
})
export class PageShell {}
