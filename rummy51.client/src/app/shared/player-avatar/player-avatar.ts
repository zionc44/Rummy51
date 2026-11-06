import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { PlayerStatus } from '../../core/lobby/lobby.models';

/**
 * A player, as a circle: their picture when there is one, their initials when
 * there is not, ringed in the colour of where they are. The size comes from
 * whoever places it, so the roster and the header can differ without a flag.
 */
@Component({
  selector: 'app-player-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './player-avatar.html',
  styleUrl: './player-avatar.scss',
  host: {
    '[class]': "'avatar--' + status()",
    // The name is always written beside it, so reading the circle aloud would
    // only repeat what follows.
    'aria-hidden': 'true',
  },
})
export class PlayerAvatar {
  readonly displayName = input.required<string>();
  readonly avatarUrl = input<string | null>(null);
  readonly status = input<PlayerStatus>('available');

  /**
   * The address that failed to load. Providers retire picture URLs, and a
   * broken image is worse than the initials it replaced.
   */
  private readonly broken = signal<string | null>(null);

  protected readonly picture = computed(() => {
    const url = this.avatarUrl();

    return url && url !== this.broken() ? url : null;
  });

  protected readonly initials = computed(() => initialsOf(this.displayName()));

  protected forgetPicture(): void {
    this.broken.set(this.avatarUrl());
  }
}

/** First letters of the first two words, or the first two letters of one. */
const initialsOf = (displayName: string): string => {
  const words = displayName.trim().split(/\s+/).filter(Boolean);

  if (words.length === 0) {
    return '?';
  }

  return words.length === 1 ? words[0].slice(0, 2) : words[0][0] + words[1][0];
};
