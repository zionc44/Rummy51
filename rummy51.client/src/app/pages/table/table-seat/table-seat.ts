import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { TableSeat as Seat } from '../../../core/lobby/lobby.models';
import { PlayerAvatar } from '../../../shared/player-avatar/player-avatar';

/**
 * One place at the table, taken or not. An empty one turns rather than sits
 * still, because the one thing a player waiting wants to know is that the
 * table is still looking.
 */
@Component({
  selector: 'app-table-seat',
  imports: [TranslatePipe, PlayerAvatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-seat.html',
  styleUrl: './table-seat.scss',
})
export class TableSeat {
  readonly seat = input.required<Seat>();

  /** Marks the signed-in player's own place, which is always the near one. */
  readonly mine = input(false);
}
