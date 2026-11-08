import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, Signal, computed, inject, input, signal, viewChild, viewChildren } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { TableSeat as Seat } from '../../../core/lobby/lobby.models';
import { PlayerAvatar } from '../../../shared/player-avatar/player-avatar';

/**
 * How many cards a line of the hand holds before the next one begins. Eight
 * is what leaves each of them enough of itself showing to be read on a phone.
 */
const PerRow = 8;

/**
 * Whether there is width for the hand in a single line. The number mirrors
 * the `sm` breakpoint in `styles/_breakpoints.scss`, which is where it lives.
 */
function oneRowFits(): Signal<boolean> {
  const query = window.matchMedia('(min-width: 640px)');
  const fits = signal(query.matches);
  const follow = (event: MediaQueryListEvent) => fits.set(event.matches);

  query.addEventListener('change', follow);
  inject(DestroyRef).onDestroy(() => query.removeEventListener('change', follow));

  return fits;
}

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
   * How many cards this hand holds when it is whole, which is more than are
   * in it while they are still being dealt.
   */
  readonly places = input(0);

  private readonly oneRow = oneRowFits();

  /**
   * Fifteen cards across a phone leave twenty pixels of each one showing,
   * which is not enough to tell one from another. Two lines double that.
   */
  private readonly splitting = computed(
    () => this.near() && !this.oneRow() && this.held().length > 0,
  );

  /**
   * The hand as it is laid. Both lines are there from the first card, so the
   * ninth one arriving does not shift the eight that went before it.
   */
  protected readonly rows = computed<readonly (readonly (string | null)[])[]>(() => {
    if (!this.splitting()) {
      return [this.held()];
    }

    const laid = this.withEveryPlace();

    return [laid.slice(0, PerRow), laid.slice(PerRow)];
  });

  /**
   * The hand with a gap kept for every card still to come. A line that is
   * already its full width can be centred under the one above it without
   * sliding out from under the cards that have landed.
   */
  private withEveryPlace(): readonly (string | null)[] {
    const cards = this.held();
    const missing = this.places() - cards.length;

    return missing > 0 ? [...cards, ...Array<null>(missing).fill(null)] : cards;
  }

  /**
   * What a line divides its width by: the places it has rather than the cards
   * in it now, so a card lands where the one before it left room for it.
   */
  protected readonly slots = computed(() => (this.splitting() ? PerRow : this.held().length));

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
