import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { GameMode } from '../../../../core/lobby/lobby.models';
import { ModeIcon } from '../../../../shared/mode-icon/mode-icon';

/** The tables open in one mode. Rows arrive with the first table. */
@Component({
  selector: 'app-table-list',
  imports: [TranslatePipe, ModeIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-list.html',
  styleUrl: './table-list.scss',
})
export class TableList {
  readonly mode = input.required<GameMode>();
  readonly count = input(0);
}
