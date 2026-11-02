import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { IconEyeOff } from '../icons/icon-eye-off/icon-eye-off';
import { IconEye } from '../icons/icon-eye/icon-eye';

/**
 * Reveals what was typed instead of asking for it twice. A confirmation field
 * only reports that two entries differ; seeing the password also catches Caps
 * Lock, the wrong keyboard layout and a trailing space.
 */
@Component({
  selector: 'app-password-visibility-toggle',
  imports: [TranslatePipe, IconEye, IconEyeOff],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './password-visibility-toggle.html',
  styleUrl: './password-visibility-toggle.scss',
})
export class PasswordVisibilityToggle {
  /** Two-way bound so the field owning the input decides its type. */
  readonly visible = model(false);
}
