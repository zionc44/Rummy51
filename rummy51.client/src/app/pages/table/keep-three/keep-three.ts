import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { HeldCard, isJoker } from '../../../core/cards/card';
import { inOrder } from '../../../core/cards/card-order';
import { pictureOf } from '../../../core/cards/deck';
import { meldsIn } from '../../../core/cards/melds';

/** Rule 12.6. */
const Kept = 3;

/** How many colours the bands run through before starting again. */
const Colours = 5;

/**
 * Rule 12.6. Laid by suit, which is the order that puts a run together -
 * including one that has just come back off the table under 12.5, whose
 * cards arrive scattered through the rest.
 */
@Component({
  selector: 'app-keep-three',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './keep-three.html',
  styleUrl: './keep-three.scss',
})
export class KeepThree {
  readonly cards = input<readonly HeldCard[]>([]);

  readonly left = input(0);

  readonly kept = output<readonly number[]>();

  protected readonly picture = pictureOf;

  protected readonly picked = signal<readonly number[]>([]);

  protected readonly laid = computed(() => inOrder(this.cards(), 'bySuit'));

  private readonly bands = computed(() => meldsIn(this.laid()));

  protected readonly enough = computed(() => this.picked().length === Kept);

  protected readonly room = computed(() => Kept - this.picked().length);

  protected chose(id: number): boolean {
    return this.picked().includes(id);
  }

  protected joker(card: HeldCard): boolean {
    return isJoker(card);
  }

  /** The colour under a card, or none where it is lying in no run. */
  protected band(at: number): string {
    const meld = this.bands()[at];

    return meld === null ? 'transparent' : `var(--meld-${(meld % Colours) + 1})`;
  }

  /** A card in or out, and never a fourth: the third has to be given up first. */
  protected pick(id: number): void {
    this.picked.update((picked) =>
      picked.includes(id)
        ? picked.filter((one) => one !== id)
        : picked.length < Kept
          ? [...picked, id]
          : picked,
    );
  }
}
