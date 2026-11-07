import { ChangeDetectionStrategy, Component, computed, linkedSignal, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import {
  CreateTableRequest,
  GAME_MODES,
  GameMode,
  JOKER_COUNTS,
  SERIES_HANDS,
  SURVIVAL_POINTS,
  TABLE_SEATS,
} from '../../../core/lobby/lobby.models';

/** Which question the third row asks, if it asks one at all. */
const LIMIT_LABELS: Readonly<Record<GameMode, string | null>> = {
  single: null,
  survival: 'lobby.pointsToFinish',
  series: 'lobby.handCount',
};

@Component({
  selector: 'app-new-table',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './new-table.html',
  styleUrl: './new-table.scss',
  host: {
    '(document:keydown.escape)': 'cancelled.emit()',
  },
})
export class NewTable {
  readonly created = output<CreateTableRequest>();
  readonly cancelled = output<void>();

  protected readonly seatChoices = TABLE_SEATS;
  protected readonly jokerChoices = JOKER_COUNTS;
  protected readonly modes = GAME_MODES;

  protected readonly seats = signal(4);
  protected readonly jokers = signal(2);
  protected readonly mode = signal<GameMode>('single');

  protected readonly limitChoices = computed<readonly number[]>(() => {
    switch (this.mode()) {
      case 'survival':
        return SURVIVAL_POINTS;
      case 'series':
        return SERIES_HANDS[this.seats()] ?? [];
      default:
        return [];
    }
  });

  protected readonly limitLabel = computed(() => LIMIT_LABELS[this.mode()]);

  /**
   * Kept across a change of mode or of player count when it is still on
   * offer, so switching from four players to three does not throw away a
   * choice of twelve hands that both of them allow.
   */
  protected readonly limit = linkedSignal<readonly number[], number | null>({
    source: this.limitChoices,
    computation: (choices, previous) => {
      if (choices.length === 0) {
        return null;
      }

      const kept = previous?.value ?? null;

      return kept !== null && choices.includes(kept) ? kept : choices[1];
    },
  });

  protected open(): void {
    this.created.emit({
      seats: this.seats(),
      jokers: this.jokers(),
      mode: this.mode(),
      limit: this.limit(),
    });
  }
}
