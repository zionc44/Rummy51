import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { FieldCodes, fieldErrorOf } from '../../core/errors/form-errors';
import { FormSubmission } from '../../core/forms/form-submission';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { FormField } from '../../shared/form-field/form-field';
import { GoogleSignInButton } from '../../shared/google-sign-in-button/google-sign-in-button';
import { PageShell } from '../../shared/page-shell/page-shell';
import { PasswordField } from '../../shared/password-field/password-field';

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
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, ErrorMessages, FormField, GoogleSignInButton, PageShell, PasswordField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    displayName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected readonly submission = new FormSubmission(this.form);
  protected readonly passwordCodes = PASSWORD_CODES;

  protected readonly displayNameError = () => fieldErrorOf(this.form.controls.displayName, DISPLAY_NAME_CODES);
  protected readonly emailError = () => fieldErrorOf(this.form.controls.email, EMAIL_CODES);

  protected submit(): void {
    this.submission.send(
      () => this.auth.register(this.form.getRawValue()),
      // Registering does not sign the player in; they continue to the form.
      () => this.router.navigate(['/login'], { queryParams: { registered: 1 } }),
    );
  }
}
