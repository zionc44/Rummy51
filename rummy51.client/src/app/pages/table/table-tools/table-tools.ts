import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { IconSort } from '../../../shared/icons/icon-sort/icon-sort';
import { Turn } from '../turn';

/**
 * Everything drawn in the board's margins rather than on it: the buttons in
 * its corners and the one line it says things with. None of it is the
 * table, and all of it is laid against the board itself, which is why this
 * holds no box of its own.
 */
@Component({
  selector: 'app-table-tools',
  imports: [TranslatePipe, IconSort],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-tools.html',
  styleUrl: './table-tools.scss',
})
export class TableTools {
  readonly turn = input.required<Turn>();

  /** Arranging a hand is nobody's turn, so the sort is there whosever it is. */
  readonly arrangeable = input(false);

  readonly winner = input<{ readonly name: string; readonly mine: boolean } | null>(null);

  readonly sorted = output<void>();

  readonly offeredRedeal = output<void>();

  readonly undid = output<void>();
}
