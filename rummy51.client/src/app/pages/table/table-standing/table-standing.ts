import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ScorePad as Pad } from '../../../core/lobby/score-pad';
import { ScorePad } from '../score-pad/score-pad';

/**
 * Rule 9.4 while the hand is still being played. The result window says
 * where the game stands when a hand ends; this is the same thing asked for
 * in the middle of one, because how close somebody is to the number is how
 * the hand in front of them should be played.
 */
@Component({
  selector: 'app-table-standing',
  imports: [TranslatePipe, ScorePad],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-standing.html',
  styleUrl: './table-standing.scss',
})
export class TableStanding {
  readonly pad = input.required<Pad>();

  /** The number rule 9.4 knocks somebody out at, so a total can be read against it. */
  readonly limit = input<number | null>(null);

  readonly closed = output<void>();
}
