import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountService } from '../../../core/account/account-service';
import { AuthService } from '../../../core/auth/auth-service';
import { FieldCodes, fieldErrorOf } from '../../../core/errors/form-errors';
import { FormSubmission } from '../../../core/forms/form-submission';
import { ErrorMessages } from '../../../shared/error-messages/error-messages';
import { FormField } from '../../../shared/form-field/form-field';

// The same rules the sign-up form answers to, in the same vocabulary.
const DISPLAY_NAME_CODES: FieldCodes = {
  required: 'DisplayNameRequired',
  minlength: 'DisplayNameLength',
  maxlength: 'DisplayNameLength',
};

@Component({
  selector: 'app-display-name-form',
  imports: [ReactiveFormsModule, TranslatePipe, ErrorMessages, FormField],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './display-name-form.html',
  styleUrl: './display-name-form.scss',
})
export class DisplayNameForm {
  private readonly account = inject(AccountService);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    displayName: [
      inject(AuthService).currentPlayer()?.displayName ?? '',
      [Validators.required, Validators.minLength(2), Validators.maxLength(50)],
    ],
  });

  protected readonly submission = new FormSubmission(this.form);
  protected readonly saved = signal(false);

  protected readonly displayNameError = () =>
    fieldErrorOf(this.form.controls.displayName, DISPLAY_NAME_CODES);

  protected submit(): void {
    this.saved.set(false);

    this.submission.send(
      () => this.account.changeDisplayName(this.form.getRawValue()),
      () => this.saved.set(true),
    );
  }
}
