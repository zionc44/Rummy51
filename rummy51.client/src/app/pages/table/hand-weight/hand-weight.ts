import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * What a series makes of one hand, as the pad marks it. The two never sit
 * together: rule 8.5 sends a redeal on from any hand 8.4 already doubles.
 */
@Component({
  selector: 'app-hand-weight',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hand-weight.html',
  styleUrl: './hand-weight.scss',
})
export class HandWeight {
  readonly fixed = input(1);

  readonly redealt = input(false);
}
