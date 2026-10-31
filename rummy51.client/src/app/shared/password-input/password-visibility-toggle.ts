import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Reveals what was typed instead of asking for it twice. A confirmation field
 * only reports that two entries differ; seeing the password also catches Caps
 * Lock, the wrong keyboard layout and a trailing space.
 */
@Component({
  selector: 'app-password-visibility-toggle',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      (click)="visible.set(!visible())"
      [attr.aria-label]="(visible() ? 'auth.hidePassword' : 'auth.showPassword') | translate"
      [attr.aria-pressed]="visible()"
      class="absolute end-2 top-1/2 -translate-y-1/2 rounded-md p-2 text-white/40
             transition hover:text-white focus-visible:outline-2 focus-visible:outline-felt-50"
    >
      <svg class="size-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
        @if (visible()) {
          <path d="M3 3l18 18M10.6 10.6a2 2 0 002.8 2.8" />
          <path d="M9.4 5.2A9.5 9.5 0 0112 5c5 0 9 4.5 9 7a12 12 0 01-2.8 3.6M6.3 6.5A12.4 12.4 0 003 12c0 2.5 4 7 9 7a9.6 9.6 0 004-.9" />
        } @else {
          <path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7Z" />
          <circle cx="12" cy="12" r="3" />
        }
      </svg>
    </button>
  `,
})
export class PasswordVisibilityToggle {
  /** Two-way bound so the field owning the input decides its type. */
  readonly visible = model(false);
}
