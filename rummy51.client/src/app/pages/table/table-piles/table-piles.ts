import { ChangeDetectionStrategy, Component, ElementRef, input, output, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { pictureOf } from '../../../core/cards/deck';
import { DrawnFrom } from '../../../core/lobby/lobby.models';
import { Turn } from '../turn';

/**
 * The corner a card is drawn from and thrown to: the finishing card, the
 * deck lying across it, and the pile beside them.
 *
 * Cards fly to and from all three, so each says where it is. The page is
 * the only thing that can see both ends of a journey, and this is one end.
 */
@Component({
  selector: 'app-table-piles',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-piles.html',
  styleUrl: './table-piles.scss',
})
export class TablePiles {
  readonly turn = input.required<Turn>();

  readonly finishing = input<string | null>(null);

  readonly stock = input(0);

  readonly discardTop = input<string | null>(null);

  readonly discardCount = input(0);

  /** True while the deck is still travelling here, so its place waits empty. */
  readonly collapsing = input(false);

  readonly drew = output<DrawnFrom>();

  /** One pile and two moves, which only the page can tell apart. */
  readonly pressed = output<void>();

  protected readonly picture = pictureOf;

  private readonly deck = viewChild<ElementRef<HTMLElement>>('deck');
  private readonly pile = viewChild<ElementRef<HTMLElement>>('pile');
  private readonly slot = viewChild<ElementRef<HTMLElement>>('slot');

  stockSpot(): HTMLElement | undefined {
    return this.deck()?.nativeElement;
  }

  discardSpot(): HTMLElement | undefined {
    return this.pile()?.nativeElement;
  }

  finishingSpot(): HTMLElement | undefined {
    return this.slot()?.nativeElement;
  }
}
