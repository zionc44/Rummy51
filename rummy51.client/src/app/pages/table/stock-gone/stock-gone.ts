import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Rule 3.5. The stock has run out and what everybody threw is about to be
 * it. There is nothing to agree to - a hand cannot go on without a stock -
 * so this only waits to be read: a deck that changed while somebody was
 * looking elsewhere is a deck they never saw.
 */
@Component({
  selector: 'app-stock-gone',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stock-gone.html',
  styleUrl: './stock-gone.scss',
})
export class StockGone {
  readonly left = input(0);

  /** Whether this player is the one the rest are now waiting for. */
  readonly waiting = input(false);

  readonly seen = output<void>();
}
