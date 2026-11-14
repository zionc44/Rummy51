import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LobbyTable } from '../../../../core/lobby/lobby.models';
import { IconJoin } from '../../../../shared/icons/icon-join/icon-join';
import { PlayerAvatar } from '../../../../shared/player-avatar/player-avatar';

/**
 * One table, read as a row of seats. How many circles there are is how many
 * players it takes, and how many of them are empty is how many it still
 * wants, so neither needs saying in words.
 */
@Component({
  selector: 'app-table-row',
  imports: [TranslatePipe, PlayerAvatar, IconJoin],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-row.html',
  styleUrl: './table-row.scss',
})
export class TableRow {
  readonly table = input.required<LobbyTable>();

  /**
   * Which table the player is sitting at, if any. One answer settles both
   * questions a row has to ask: whether this is theirs, and whether there is
   * anything left to offer them.
   */
  readonly seatedAt = input<string | null>(null);

  readonly joined = output<string>();

  protected readonly mine = computed(() => this.seatedAt() === this.table().tableId);

  protected readonly canJoin = computed(() => this.seatedAt() === null);

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
