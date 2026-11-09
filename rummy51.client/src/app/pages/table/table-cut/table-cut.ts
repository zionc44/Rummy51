import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  viewChildren,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { revealedAt, revealedOrder } from '../../../core/cards/the-cut';
import { HandView } from '../../../core/lobby/lobby.models';

/**
 * How many rows the deck is laid out in for the cut. Eighteen cards to a row
 * at most, which is what leaves each of them enough width to be told apart
 * and aimed at.
 */
const FanRows = 6;

/**
 * The deck laid out face down for the cutter to choose from, and the cards
 * the cut turns up where they lie.
 *
 * It shows and it reports where somebody clicked. Whose turn it is, and
 * what becomes of the cards afterwards, belong to the table.
 */
@Component({
  selector: 'app-table-cut',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'cut', '[class.cut--sweeping]': 'sweeping()' },
  templateUrl: './table-cut.html',
  styleUrl: './table-cut.scss',
})
export class TableCut {
  readonly hand = input.required<HandView>();

  /** How many of the cards the cut turned up have been turned over so far. */
  readonly turned = input(0);

  /** The deck is gathering itself away, so nothing here can be clicked. */
  readonly sweeping = input(false);

  /** Whether this viewer is the one who cuts. */
  readonly yours = input(false);

  /** Whose cut it is, for the line above the fan. */
  readonly cutter = input('');

  readonly cut = output<number>();

  private readonly fanCards = viewChildren<ElementRef<HTMLElement>>('fanCard');

  /** The cards as they lie, for whoever has to fly them somewhere else. */
  cards(): readonly HTMLElement[] {
    return this.fanCards().map((card) => card.nativeElement);
  }

  /** Where the cut turned something up, and what it turned up there. */
  private readonly turnedUp = computed(() => revealedOrder(this.hand()));
  private readonly facesUp = computed(() => revealedAt(this.hand()));

  /**
   * The jokers, shown where they lie so a cut onto one can be aimed at. One
   * falls at the cut about twice in a hundred hands, which is no way to work
   * on that path. Empty unless the server was asked to send them.
   */
  protected readonly jokers = computed(() => new Set(this.hand().jokerPositions));

  protected readonly takeable = computed(() => this.yours() && !this.sweeping());

  /** The face a position turned up, or null while it is still face down. */
  protected faceAt(position: number): string | null {
    return this.facesUp().get(position) ?? null;
  }

  /** Whether the card lying there has had its turn yet. */
  protected isTurned(position: number): boolean {
    const step = this.turnedUp().indexOf(position);

    return step !== -1 && step < this.turned();
  }

  /**
   * The one card the sequence is on. It belongs above every other, its
   * neighbours in the fan and the cards the cut turned up before it alike,
   * or it turns over half hidden beneath the one lying on top of it.
   */
  protected isTurning(position: number): boolean {
    return this.turnedUp()[this.turned() - 1] === position;
  }

  protected readonly perRow = computed(() => Math.ceil(this.hand().deckSize / FanRows));

  /**
   * The deck laid out for the cut. One row is unclickable on a phone - a
   * hundred cards across three hundred pixels is three pixels each - so it
   * is folded into the height there is.
   */
  protected readonly fan = computed(() => {
    const size = this.hand().deckSize;
    const perRow = this.perRow();

    return Array.from({ length: FanRows }, (_, row) =>
      Array.from(
        { length: Math.max(0, Math.min(perRow, size - row * perRow)) },
        (_, column) => row * perRow + column,
      ),
    ).filter((row) => row.length > 0);
  });
}
