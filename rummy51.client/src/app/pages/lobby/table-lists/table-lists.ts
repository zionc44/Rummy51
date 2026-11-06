import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { GAME_MODES, GameMode } from '../../../core/lobby/lobby.models';
import { ModeIcon } from '../../../shared/mode-icon/mode-icon';
import { TableList } from './table-list/table-list';

/**
 * All three lists are rendered always. From `sm` upwards there is room for all
 * three side by side and the tabs disappear; on a phone the tabs become an
 * accordion — the open one takes the width it needs, the others shrink to an
 * icon and their count. Which of the two applies is decided in the stylesheet,
 * so the template and the state stay the same at every width.
 */
@Component({
  selector: 'app-table-lists',
  imports: [TranslatePipe, ModeIcon, TableList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-lists.html',
  styleUrl: './table-lists.scss',
})
export class TableLists {
  protected readonly modes = GAME_MODES;

  /** Only consulted on a phone; wider screens show every list. */
  protected readonly openMode = signal<GameMode>('discrete');

  /** Zero everywhere until tables exist; the hub will feed this. */
  protected readonly counts = signal<Record<GameMode, number>>({
    discrete: 0,
    survival: 0,
    series: 0,
  });
}
