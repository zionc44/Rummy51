import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PadColumn, PadRow } from '../../../core/lobby/score-pad';
import { HandWeight } from '../hand-weight/hand-weight';

/**
 * Rule 9.2 as a score pad: a column to a player, a row to a hand, and in
 * each cell what that hand cost them over what they stood at once it was
 * counted. It renders and decides nothing - `scorePadOf` has laid it out.
 */
@Component({
  selector: 'app-score-pad',
  imports: [TranslatePipe, HandWeight],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './score-pad.html',
  styleUrl: './score-pad.scss',
})
export class ScorePad {
  readonly columns = input<readonly PadColumn[]>([]);

  readonly rows = input<readonly PadRow[]>([]);

  /** Room beside the hand's number only where some hand needs it. */
  protected readonly weighted = computed(() =>
    this.rows().some((row) => row.fixed > 1 || row.redealt),
  );
}
