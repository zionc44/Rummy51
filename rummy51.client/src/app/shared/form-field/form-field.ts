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
  template: `
    <div class="flex flex-col gap-1.5">
      <label [attr.for]="controlId()" class="text-sm font-medium text-white/80">
        {{ label() | translate }}
      </label>

      <ng-content />

      @if (hint(); as hintKey) {
        <p class="text-xs text-white/40">{{ hintKey | translate }}</p>
      }

      <app-field-error [error]="error()" />
    </div>
  `,
})
export class FormField {
  readonly controlId = input.required<string>();
  readonly label = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly error = input<ApiError | null>(null);
}
