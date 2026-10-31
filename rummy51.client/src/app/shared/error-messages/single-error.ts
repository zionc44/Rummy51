import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ErrorMessages } from './error-messages';
import { ApiError } from '../../core/errors/api-error';

/** Convenience wrapper for the one error a single form field can show. */
@Component({
  selector: 'app-field-error',
  imports: [ErrorMessages],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<app-error-messages [errors]="asList()" />',
})
export class FieldError {
  readonly error = input<ApiError | null>(null);

  protected readonly asList = computed(() => {
    const error = this.error();
    return error ? [error] : [];
  });
}
