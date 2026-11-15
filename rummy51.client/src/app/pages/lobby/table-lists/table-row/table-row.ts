import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LobbyTable } from '../../../../core/lobby/lobby.models';
import { IconJoin } from '../../../../shared/icons/icon-join/icon-join';
import { IconReturn } from '../../../../shared/icons/icon-return/icon-return';
import { PlayerAvatar } from '../../../../shared/player-avatar/player-avatar';

/**
 * One table, read as a row of seats. How many circles there are is how many
 * players it takes, and how many of them are empty is how many it still
 * wants, so neither needs saying in words.
 */
@Component({
  selector: 'app-table-row',
  imports: [TranslatePipe, PlayerAvatar, IconJoin, IconReturn],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-row.html',
  styleUrl: './table-row.scss',
})
export class TableRow {
  readonly table = input.required<LobbyTable>();

  /** Rule 10.1: a chair here is already theirs, so the way in is back in. */
  readonly mine = input(false);

  /** Waiting at a table somewhere, which is the one seat they get only one of. */
  readonly waiting = input(false);

  readonly joined = output<string>();
  readonly returned = output<string>();

  protected readonly canJoin = computed(() => !this.waiting());

  /** What the mode asks for, which a single hand does not ask at all. */
  protected readonly detail = computed(() => {
    const { mode, limit } = this.table();

    if (limit === null) {
      return null;
    }

    return {
      key: mode === 'survival' ? 'lobby.points' : 'lobby.hands',
      amount: { '0': String(limit) },
    };
  });
}
