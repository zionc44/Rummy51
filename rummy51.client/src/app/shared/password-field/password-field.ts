import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { FieldCodes, fieldErrorOf } from '../../core/errors/form-errors';
import { FormField } from '../form-field/form-field';
import { PasswordVisibilityToggle } from '../password-visibility-toggle/password-visibility-toggle';

const REQUIRED_ONLY: FieldCodes = { required: 'PasswordRequired' };

/**
 * A password: its label, the button that reveals it, and the rules it reports.
 * Five forms drew this identically, and everything that differed between them
 * is an input below.
 */
@Component({
  selector: 'app-password-field',
  imports: [ReactiveFormsModule, FormField, PasswordVisibilityToggle],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './password-field.html',
  styleUrl: './password-field.scss',
})
export class PasswordField {
  readonly control = input.required<FormControl<string>>();
  readonly controlId = input.required<string>();
  readonly label = input('auth.password');

  /** Tells a password manager whether this is the known one or a new one. */
  readonly autocomplete = input<'current-password' | 'new-password'>('current-password');

  /** Which code each failed rule reports, since the same field is not asked
      the same questions on every form. */
  readonly codes = input<FieldCodes>(REQUIRED_ONLY);

  protected readonly visible = signal(false);

  protected readonly error = () => fieldErrorOf(this.control(), this.codes());
}
