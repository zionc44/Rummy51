import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { GameMode, LobbyTable } from '../../../../core/lobby/lobby.models';
import { ModeIcon } from '../../../../shared/mode-icon/mode-icon';
import { TableRow } from '../table-row/table-row';

/** The tables open in one mode. */
@Component({
  selector: 'app-table-list',
  imports: [TranslatePipe, ModeIcon, TableRow],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-list.html',
  styleUrl: './table-list.scss',
})
export class TableList {
  readonly mode = input.required<GameMode>();
  readonly tables = input.required<readonly LobbyTable[]>();

  /** Passed along: a row needs both to know what it may offer. */
  readonly seatedAt = input<ReadonlySet<string>>(new Set());
  readonly waiting = input(false);

  readonly joined = output<string>();
  readonly returned = output<string>();

  /**
   * A table in play keeps its place in the list but sinks below the ones that
   * can still be joined, so the top of the list is always what is on offer.
   */
  protected readonly rows = computed(() =>
    [...this.tables()].sort((first, second) => Number(first.started) - Number(second.started)),
  );
}
