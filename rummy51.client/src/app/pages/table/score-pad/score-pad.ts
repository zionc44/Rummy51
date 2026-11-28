import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PadColumn, PadRow } from '../../../core/lobby/score-pad';

/**
 * Rule 9.2 as a score pad: a column to a player, a row to a hand, and in
 * each cell what that hand cost them over what they stood at once it was
 * counted. It renders and decides nothing - `scorePadOf` has laid it out.
 */
@Component({
  selector: 'app-score-pad',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './score-pad.html',
  styleUrl: './score-pad.scss',
})
export class ScorePad {
  readonly columns = input<readonly PadColumn[]>([]);

  readonly rows = input<readonly PadRow[]>([]);
}
