import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,

  viewChild,
  viewChildren,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { HeldCard } from '../../../core/cards/card';
import { pictureOf } from '../../../core/cards/deck';
import { TableSeat as Seat } from '../../../core/lobby/lobby.models';
import { PlayerAvatar } from '../../../shared/player-avatar/player-avatar';

/** Once round a circle of radius forty-six, which is the one the ring draws. */
const Round = 2 * Math.PI * 46;

/** Half the wait and a quarter of it, which are the two worth noticing. */
const Half = 0.5;

const Quarter = 0.25;

/**
 * One place at the table, taken or not: who is sitting there, whether they
 * are dealing, what they are holding that the table may see, and what they
 * are holding that it may not. An empty place turns rather than sits still,
 * because the one thing a player waiting wants to know is that the table is
 * still looking.
 *
 * The player's own hand is not here. It is `app-player-hand`, laid beside
 * this one along the bottom of the screen.
 */
@Component({
  selector: 'app-table-seat',
  imports: [TranslatePipe, PlayerAvatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class.seat--compact]': 'compact()' },
  templateUrl: './table-seat.html',
  styleUrl: './table-seat.scss',
})
export class TableSeat {
  readonly seat = input.required<Seat>();

  /** Marks the signed-in player's own place, which is always the near one. */
  readonly mine = input(false);

  /** Whose turn it is to deal. It moves on by one with every hand. */
  readonly dealer = input(false);

  /**
   * Drawn small and without its name. The near place has the width of the
   * table, and on a phone that line is worth more to the cards beside it
   * than to a name its owner already knows.
   */
  readonly compact = input(false);

  /**
   * Cards this player is holding face up for the table to read - the jokers
   * the cut gave them. A null is one still on its way, drawn as a gap, so
   * the row is already the width it will be when the last one lands.
   */
  readonly held = input<readonly (HeldCard | null)[]>([]);

  /**
   * How many cards this player is holding. Across the table that is a pile
   * of backs with the number on it; at the near place the cards are already
   * there to be seen, so it is only the number, under the picture. Null
   * before the deal, when there is nothing to say.
   */
  readonly holding = input<number | null>(null);

  /**
   * How long this player has left to act, and out of how long. Null when
   * the table is not waiting for them. It says nothing about what they are
   * being waited for, and does not need to: cutting, drawing and discarding
   * are all somebody taking too long over something.
   */
  readonly clock = input<{ readonly left: number; readonly of: number } | null>(null);

  /** Counted down by the page rather than measured here. */
  readonly left = input(0);

  protected readonly round = Round;

  /**
   * Drawn from the number, not animated. Setting an animation's delay again
   * while it runs does not start it over - the browser adds the new delay
   * to what has already run - so every message during a turn pushed the arc
   * on until it had emptied with the wait still going.
   */
  protected readonly ring = computed(() => {
    const clock = this.clock();

    if (clock === null) {
      return null;
    }

    const left = clock.of === 0 ? 0 : Math.max(0, Math.min(1, this.left() / clock.of));

    return {
      // Negative, which empties the ring from where it starts and so the
      // way a clock goes. Positive eats it from the far end, which is the
      // same arc disappearing backwards.
      gone: -(Round * (1 - left)),
      pressing: left <= Half,
      urgent: left <= Quarter,
    };
  });

  protected readonly picture = pictureOf;

  private readonly landings = viewChildren<ElementRef<HTMLElement>>('landing');

  private readonly pile = viewChild<ElementRef<HTMLElement>>('pile');

  private readonly back = viewChild<ElementRef<HTMLElement>>('back');

  /** Where a card flying to this seat comes to rest, place by place. */
  landingSpot(step: number): HTMLElement | undefined {
    return this.landings()[step]?.nativeElement;
  }

  /** Where a card nobody may see comes to rest instead. */
  pileSpot(): HTMLElement | undefined {
    return this.pile()?.nativeElement;
  }

  /**
   * The back lying on top of that pile. It is what a card becomes on its
   * way into a hand nobody may look at - and the pile itself cannot stand
   * in for it, because a pile carries the count as well as the cards.
   */
  backSpot(): HTMLElement | undefined {
    return this.back()?.nativeElement;
  }
}
