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

  /** Passed along: a row needs it to know whether it is theirs. */
  readonly seatedAt = input<string | null>(null);
  readonly joined = output<string>();

  /**
   * A table in play keeps its place in the list but sinks below the ones that
   * can still be joined, so the top of the list is always what is on offer.
   */
  protected readonly rows = computed(() =>
    [...this.tables()].sort((first, second) => Number(first.started) - Number(second.started)),
  );
}
