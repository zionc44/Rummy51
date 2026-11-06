import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { FormSubmission } from '../../core/forms/form-submission';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { PageShell } from '../../shared/page-shell/page-shell';
import { PasswordField } from '../../shared/password-field/password-field';

/**
 * Where Google sends someone whose address is already registered with a
 * password. The Google identity itself is held server-side; all this page
 * sends is the proof that the account is theirs.
 */
@Component({
  selector: 'app-link-google',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, ErrorMessages, PageShell, PasswordField],
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

  protected readonly submission = new FormSubmission(this.form);

  protected submit(): void {
    this.submission.send(
      () => this.auth.linkGoogle(this.form.getRawValue().password),
      () => this.router.navigate(['/lobby']),
    );
  }
}
