import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ApiError } from '../../core/errors/api-error';
import { FieldError } from '../error-messages/single-error';

/**
 * Label, hint and error around a projected input. The input stays with the
 * form that owns it, so formControlName keeps working and this component needs
 * to know nothing about forms.
 */
@Component({
  selector: 'app-form-field',
  imports: [TranslatePipe, FieldError],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './form-field.html',
  styleUrl: './form-field.scss',
})
export class FormField {
  readonly controlId = input.required<string>();
  readonly label = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly error = input<ApiError | null>(null);
}
