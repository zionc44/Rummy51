import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AccountService } from '../../../core/account/account-service';
import { AuthService } from '../../../core/auth/auth-service';
import { PlayerSummary } from '../../../core/auth/auth.models';
import { FieldCodes } from '../../../core/errors/form-errors';
import { FormSubmission } from '../../../core/forms/form-submission';
import { ErrorMessages } from '../../../shared/error-messages/error-messages';
import { PasswordField } from '../../../shared/password-field/password-field';

const CURRENT_PASSWORD_CODES: FieldCodes = { required: 'CurrentPasswordRequired' };

const NEW_PASSWORD_CODES: FieldCodes = {
  required: 'PasswordRequired',
  minlength: 'PasswordTooShort',
};

/**
 * Setting the first password and replacing an existing one are one form, not
 * two: they differ by a single field, and which of them applies flips the
 * moment the first succeeds.
 */
@Component({
  selector: 'app-password-form',
  imports: [ReactiveFormsModule, TranslatePipe, ErrorMessages, PasswordField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './password-form.html',
  styleUrl: './password-form.scss',
})
export class PasswordForm {
  private readonly auth = inject(AuthService);
  private readonly account = inject(AccountService);

  protected readonly hasPassword = computed(() => this.auth.currentPlayer()?.hasPassword ?? false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    currentPassword: ['', [requiredWhen(() => this.hasPassword())]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected readonly submission = new FormSubmission(this.form);
  protected readonly saved = signal(false);

  protected readonly currentPasswordCodes = CURRENT_PASSWORD_CODES;
  protected readonly newPasswordCodes = NEW_PASSWORD_CODES;

  protected submit(): void {
    this.saved.set(false);

    this.submission.send(() => this.change(), () => this.acceptSaved());
  }

  private change(): Observable<PlayerSummary> {
    const { currentPassword, newPassword } = this.form.getRawValue();

    return this.hasPassword()
      ? this.account.changePassword({ currentPassword, newPassword })
      : this.account.setPassword({ password: newPassword });
  }

  /**
   * Clearing the fields matters more here than elsewhere: what is left in them
   * is the password itself. The first success also turns this into a change
   * form, so the current-password rule has to be weighed again.
   */
  private acceptSaved(): void {
    this.form.reset();
    this.form.controls.currentPassword.updateValueAndValidity();

    this.saved.set(true);
  }
}

/** Required only while the condition holds, which is read at validation time. */
const requiredWhen = (applies: () => boolean): ValidatorFn =>
  (control: AbstractControl) => (applies() ? Validators.required(control) : null);
