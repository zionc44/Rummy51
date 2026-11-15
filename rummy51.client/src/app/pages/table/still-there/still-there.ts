import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Rule 10.6. The table has decided this chair is empty of a person, and the
 * person is looking straight at it. Answering is the whole of coming back -
 * there is nothing to agree to and nothing to undo, so there is one button
 * and no way to dismiss it.
 */
@Component({
  selector: 'app-still-there',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './still-there.html',
  styleUrl: './still-there.scss',
})
export class StillThere {
  readonly answered = output<void>();
}
