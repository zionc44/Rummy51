import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * What a hand is worth and what it could do, beside the hand itself.
 *
 * Three of the four are read off the hand as its owner has laid it out,
 * which is the same reading the coloured bands come from: a run is a run
 * because the cards are side by side, and a hand nobody has arranged shows
 * nothing. That is not a shortcoming - arranging the hand is how a player
 * works out what they have, and these say where that has got to.
 */
@Component({
  selector: 'app-table-tally',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-tally.html',
  styleUrl: './table-tally.scss',
})
export class TableTally {
  /** What the hand costs if it ends now. A hundred until its owner is down. */
  readonly worth = input(0);

  /** What the melds in it come to, counted as the fifty-one counts. */
  readonly points = input(0);

  readonly clean = input(false);

  readonly canGoDown = input(false);

  /** Once down, the threshold is behind them and only the cost still counts. */
  readonly wentDown = input(false);

  /**
   * Whether the hand is worth more than it would be if its owner went down.
   * A hundred is the price of never showing, and it is nearly always worse.
   */
  protected readonly costly = computed(() => !this.wentDown());
}
