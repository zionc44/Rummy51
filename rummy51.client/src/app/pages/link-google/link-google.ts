import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { ApiError } from '../../core/errors/api-error';
import { FieldCodes, fieldErrorOf } from '../../core/errors/form-errors';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { FormField } from '../../shared/form-field/form-field';
import { PageShell } from '../../shared/page-shell/page-shell';
import { PasswordVisibilityToggle } from '../../shared/password-visibility-toggle/password-visibility-toggle';

const PASSWORD_CODES: FieldCodes = {
  required: 'PasswordRequired',
};

/**
 * Where Google sends someone whose address is already registered with a
 * password. The Google identity itself is held server-side; all this page
 * sends is the proof that the account is theirs.
 */
@Component({
  selector: 'app-link-google',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, ErrorMessages, FormField, PageShell, PasswordVisibilityToggle],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './link-google.html',
  styleUrl: './link-google.scss',
})
export class LinkGoogle {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Shown so the player knows which account they are about to join. */
  readonly email = input('');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    password: ['', [Validators.required]],
  });

  protected readonly passwordVisible = signal(false);
  protected readonly submitting = signal(false);
  protected readonly serverErrors = signal<readonly ApiError[]>([]);

  protected readonly passwordError = () => fieldErrorOf(this.form.controls.password, PASSWORD_CODES);

  protected submit(): void {
    this.serverErrors.set([]);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);

    this.auth.linkGoogle(this.form.getRawValue().password).subscribe({
      next: () => this.router.navigate(['/lobby']),
      error: (errors: ApiError[]) => {
        this.serverErrors.set(errors);
        this.submitting.set(false);
      },
    });
  }
}
