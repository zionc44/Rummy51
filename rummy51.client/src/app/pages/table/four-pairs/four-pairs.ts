import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { pictureOf } from '../../../core/cards/deck';

/** Rule 12.9's eight cards, laid out to be read and not touched. */
@Component({
  selector: 'app-four-pairs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './four-pairs.html',
  styleUrl: './four-pairs.scss',
})
export class FourPairs {
  /** Flat, as the server sends them: the same code twice, four times over. */
  readonly cards = input<readonly string[]>([]);

  protected readonly picture = pictureOf;

  /** Tracked by position everywhere below, a pair being one code twice. */
  protected readonly pairs = computed(() => {
    const cards = this.cards();

    return Array.from({ length: cards.length / 2 }, (_, at) => cards.slice(at * 2, at * 2 + 2));
  });
}
