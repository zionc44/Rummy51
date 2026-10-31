import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ApiError, interpolationOf, translationKeyOf } from '../../core/errors/api-error';

/**
 * Renders failures the server or a validator reported. Both describe a failure
 * the same way — a code and its parameters — so one component shows either.
 */
@Component({
  selector: 'app-error-messages',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (messages().length) {
      <div role="alert" class="flex flex-col gap-1">
        @for (message of messages(); track message.key) {
          <p class="text-sm text-red-300">{{ message.key | translate: message.values }}</p>
        }
      </div>
    }
  `,
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
