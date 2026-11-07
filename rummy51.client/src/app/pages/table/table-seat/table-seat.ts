import { ChangeDetectionStrategy, Component, ElementRef, input, viewChild, viewChildren } from '@angular/core';
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
  host: { '[class.seat--near]': 'near()' },
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
   * The place along the bottom of the screen. It has the width of the table
   * to itself, so the picture goes to the far side and the cards take the
   * rest of the line - which is where this player's hand will be laid.
   */
  readonly near = input(false);

  /**
   * Cards this player is holding face up for the table to read - the jokers
   * the cut gave them. A null is one still on its way, drawn as a gap, so
   * the row is already the width it will be when the last one lands.
   */
  readonly held = input<readonly (string | null)[]>([]);

  /**
   * How many cards this player is holding. Across the table that is a pile
   * of backs with the number on it; at the near place the cards are already
   * there to be seen, so it is only the number, under the picture. Null
   * before the deal, when there is nothing to say.
   */
  readonly holding = input<number | null>(null);

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
