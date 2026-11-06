import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { ApiError } from '../../core/errors/api-error';
import { FieldCodes, fieldErrorOf } from '../../core/errors/form-errors';
import { FormSubmission } from '../../core/forms/form-submission';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { FormField } from '../../shared/form-field/form-field';
import { GoogleSignInButton } from '../../shared/google-sign-in-button/google-sign-in-button';
import { PageShell } from '../../shared/page-shell/page-shell';
import { PasswordField } from '../../shared/password-field/password-field';

const EMAIL_CODES: FieldCodes = {
  required: 'EmailRequired',
  email: 'EmailInvalid',
};

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, ErrorMessages, FormField, GoogleSignInButton, PageShell, PasswordField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Set by the sign-up form on its way here, so the good news survives a refresh. */
  readonly registered = input(false, { transform: (value: unknown) => value === '1' || value === true });

  /**
   * Why the Google callback sent the player back. It is a redirect rather than
   * a response, so this query parameter is the only way the failure reaches
   * the page at all.
   */
  readonly error = input<string>();

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [false],
  });

  protected readonly submission = new FormSubmission(this.form);

  // The redirect's complaint stands until the player tries for themselves.
  private readonly redirectError = linkedSignal<readonly ApiError[]>(() => {
    const code = this.error();

    return code ? [{ code, parameters: [] }] : [];
  });

  protected readonly errors = computed(() => {
    const reported = this.submission.errors();

    return reported.length ? reported : this.redirectError();
  });

  protected readonly emailError = () => fieldErrorOf(this.form.controls.email, EMAIL_CODES);

  protected submit(): void {
    this.redirectError.set([]);

    this.submission.send(
      () => this.auth.login(this.form.getRawValue()),
      () => this.router.navigate(['/lobby']),
    );
  }
}
