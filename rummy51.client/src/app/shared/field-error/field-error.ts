import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ApiError } from '../../core/errors/api-error';
import { ErrorMessages } from '../error-messages/error-messages';

/** Convenience wrapper for the one error a single form field can show. */
@Component({
  selector: 'app-field-error',
  imports: [ErrorMessages],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './field-error.html',
  styleUrl: './field-error.scss',
})
export class FieldError {
  readonly error = input<ApiError | null>(null);

  protected readonly asList = computed(() => {
    const error = this.error();
    return error ? [error] : [];
  });
}
