import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ApiError } from '../../core/errors/api-error';
import { ErrorMessages } from '../error-messages/error-messages';

/**
 * Something that has to be read before play goes on, said in the middle of
 * the screen with one thing to press.
 *
 * It is not where a form shows its failures. A form's sit beside the field
 * they belong to, and the player is already looking at the field. A move a
 * table refused comes back while they are looking at their cards, and a
 * line under the board is a line nobody reads.
 *
 * What the words are is somebody else's job: the same component that words
 * a failure anywhere else words it here.
 */
@Component({
  selector: 'app-notice',
  imports: [TranslatePipe, ErrorMessages],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './notice.html',
  styleUrl: './notice.scss',
})
export class Notice {
  readonly errors = input<readonly ApiError[]>([]);

  readonly closed = output();
}
