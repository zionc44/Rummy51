import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { ApiError } from '../../core/errors/api-error';
import { FieldCodes, fieldErrorOf } from '../../core/errors/form-errors';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { FieldError } from '../../shared/error-messages/single-error';
import { LanguageSwitcher } from '../../shared/language-switcher/language-switcher';
import { PasswordVisibilityToggle } from '../../shared/password-input/password-visibility-toggle';

// The rules mirror the server's. Validating here answers before the round
// trip; the server still decides, and says so in the same vocabulary.
const DISPLAY_NAME_CODES: FieldCodes = {
  required: 'DisplayNameRequired',
  minlength: 'DisplayNameLength',
  maxlength: 'DisplayNameLength',
};

const EMAIL_CODES: FieldCodes = {
  required: 'EmailRequired',
  email: 'EmailInvalid',
  maxlength: 'EmailTooLong',
};

const PASSWORD_CODES: FieldCodes = {
  required: 'PasswordRequired',
  minlength: 'PasswordTooShort',
};

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, ErrorMessages, FieldError, LanguageSwitcher, PasswordVisibilityToggle],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.html',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    displayName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected readonly passwordVisible = signal(false);
  protected readonly submitting = signal(false);
  protected readonly serverErrors = signal<readonly ApiError[]>([]);

  protected readonly displayNameError = () => fieldErrorOf(this.form.controls.displayName, DISPLAY_NAME_CODES);
  protected readonly emailError = () => fieldErrorOf(this.form.controls.email, EMAIL_CODES);
  protected readonly passwordError = () => fieldErrorOf(this.form.controls.password, PASSWORD_CODES);

  protected submit(): void {
    this.serverErrors.set([]);

    if (this.form.invalid) {
      // Nothing was touched if the player pressed enter straight away, and an
      // untouched control stays quiet. Mark them so the reasons appear.
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/login'], { queryParams: { registered: 1 } }),
      error: (errors: ApiError[]) => {
        this.serverErrors.set(errors);
        this.submitting.set(false);
      },
    });
  }
}
