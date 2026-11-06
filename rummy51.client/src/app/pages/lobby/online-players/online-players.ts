import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LobbyPlayer } from '../../../core/lobby/lobby.models';

/**
 * One name per line with a ring showing where the player is. The column keeps
 * its own scroll, so a crowded lobby does not push the tables off the screen.
 */
@Component({
  selector: 'app-online-players',
  imports: [TranslatePipe],
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
      initials: initialsOf(player.displayName),
      isSelf: player.playerId === this.currentPlayerId(),
    })),
  );
}

/** First letters of the first two words, or the first two letters of one. */
const initialsOf = (displayName: string): string => {
  const words = displayName.trim().split(/\s+/).filter(Boolean);

  if (words.length === 0) {
    return '?';
  }

  return words.length === 1 ? words[0].slice(0, 2) : words[0][0] + words[1][0];
};
