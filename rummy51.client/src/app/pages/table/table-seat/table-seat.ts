import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  viewChild,
  viewChildren,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { HeldCard } from '../../../core/cards/card';
import { TableSeat as Seat } from '../../../core/lobby/lobby.models';
import { IconSort } from '../../../shared/icons/icon-sort/icon-sort';
import { PlayerAvatar } from '../../../shared/player-avatar/player-avatar';

/** A card shown here is seen whole, so it is read from the whole-card deck. */
const Deck = 'cards/';

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
  imports: [TranslatePipe, IconSort, PlayerAvatar],
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

  /**
   * The wait as the ring draws it: how long it runs for, and how far into
   * it to start. The second is negative, which is what lets a page opened
   * halfway through a wait show the arc that is actually left rather than
   * a full one.
   */
  protected readonly ring = computed(() => {
    const clock = this.clock();

    if (clock === null) {
      return null;
    }

    return {
      wait: `${clock.of.toFixed(2)}s`,
      elapsed: `${(clock.left - clock.of).toFixed(2)}s`,
      // When the last quarter begins, counted from now. Negative once it
      // already has, which is what makes a page opened late flash at once.
      flash: `${(clock.left - clock.of * 0.25).toFixed(2)}s`,
    };
  });

  /** Only your own hand can be sorted, and only while you are holding one. */
  readonly sortable = input(false);

  readonly sorted = output();

  protected readonly deck = Deck;

  private readonly landings = viewChildren<ElementRef<HTMLElement>>('landing');

  private readonly pile = viewChild<ElementRef<HTMLElement>>('pile');

  /** Where a card flying to this seat comes to rest, place by place. */
  landingSpot(step: number): HTMLElement | undefined {
    return this.landings()[step]?.nativeElement;
  }

  /** Where a card nobody may see comes to rest instead. */
  pileSpot(): HTMLElement | undefined {
    return this.pile()?.nativeElement;
  }
}
