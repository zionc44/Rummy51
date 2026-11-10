import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { pictureOf } from '../../../core/cards/deck';
import { MeldView } from '../../../core/lobby/lobby.models';

/**
 * What is lying on the table, in the order it was laid there. Melds are not
 * gathered by whose they are: on a real table they are not, and anybody who
 * has gone down may add to any of them, so sorting them by owner would say
 * something about them that is not true.
 */
@Component({
  selector: 'app-table-melds',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-melds.html',
  styleUrl: './table-melds.scss',
})
export class TableMelds {
  readonly melds = input<readonly MeldView[]>([]);

  protected readonly picture = pictureOf;

  /**
   * How wide a laid card is drawn. The board does not grow to hold more -
   * it is the screen, and the screen is what it is - so what gives way as
   * the table fills is the cards. The steps are measured at a phone's
   * width: the first holds about a dozen runs of three, and the last holds
   * everything four hands between them can put down.
   */
  protected readonly size = computed(() => {
    const laid = this.melds().reduce((all, meld) => all + meld.cards.length, 0);

    if (laid <= 36) {
      return '2.5rem';
    }

    return laid <= 56 ? '2rem' : '1.6rem';
  });
}
