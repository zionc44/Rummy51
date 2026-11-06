import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LobbyPlayer } from '../../../core/lobby/lobby.models';
import { PlayerAvatar } from '../../../shared/player-avatar/player-avatar';

/**
 * One player per line: their picture or their initials, ringed in the colour
 * of where they are. The column keeps its own scroll, so a crowded lobby does
 * not push the tables off the screen.
 */
@Component({
  selector: 'app-online-players',
  imports: [TranslatePipe, PlayerAvatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './online-players.html',
  styleUrl: './online-players.scss',
})
export class OnlinePlayers {
  readonly players = input.required<readonly LobbyPlayer[]>();
  readonly currentPlayerId = input<string | null>(null);

  protected readonly rows = computed(() =>
    this.players().map((player) => ({
      ...player,
      isSelf: player.playerId === this.currentPlayerId(),
    })),
  );
}
