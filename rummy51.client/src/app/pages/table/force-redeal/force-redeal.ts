import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FourPairs } from '../four-pairs/four-pairs';

/**
 * Rule 12.10 put to whoever may force: the table asked, or the deal ended
 * where it stands. Nothing has been sent yet, so the third way out is to
 * have asked for nothing.
 */
@Component({
  selector: 'app-force-redeal',
  imports: [TranslatePipe, FourPairs],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './force-redeal.html',
  styleUrl: './force-redeal.scss',
})
export class ForceRedeal {
  readonly pairs = input<readonly string[]>([]);

  /** True ends the deal there and then; false puts it to the table. */
  readonly forced = output<boolean>();

  readonly cancelled = output<void>();
}
