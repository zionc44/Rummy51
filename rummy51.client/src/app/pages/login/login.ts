import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
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

const EMAIL_CODES: FieldCodes = {
  required: 'EmailRequired',
  email: 'EmailInvalid',
};

const PASSWORD_CODES: FieldCodes = {
  required: 'PasswordRequired',
};

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, ErrorMessages, FieldError, LanguageSwitcher, PasswordVisibilityToggle],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Set by the sign-up form on its way here, so the good news survives a refresh. */
  readonly registered = input(false, { transform: (value: unknown) => value === '1' || value === true });

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [false],
  });

  protected readonly passwordVisible = signal(false);
  protected readonly submitting = signal(false);
  protected readonly serverErrors = signal<readonly ApiError[]>([]);

  protected readonly emailError = () => fieldErrorOf(this.form.controls.email, EMAIL_CODES);
  protected readonly passwordError = () => fieldErrorOf(this.form.controls.password, PASSWORD_CODES);

  protected submit(): void {
    this.serverErrors.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/lobby']),
      error: (errors: ApiError[]) => {
        this.serverErrors.set(errors);
        this.submitting.set(false);
      },
    });
  }
}
