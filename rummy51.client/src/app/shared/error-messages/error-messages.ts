import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ApiError, interpolationOf, translationKeyOf } from '../../core/errors/api-error';

/**
 * Renders failures the server or a validator reported. Both describe a failure
 * the same way - a code and its parameters - so one component shows either.
 */
@Component({
  selector: 'app-error-messages',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './error-messages.html',
  styleUrl: './error-messages.scss',
})
export class ErrorMessages {
  readonly errors = input<readonly ApiError[]>([]);

  protected readonly messages = computed(() =>
    this.errors().map((error) => ({
      key: translationKeyOf(error),
      values: interpolationOf(error),
    })),
  );
}
