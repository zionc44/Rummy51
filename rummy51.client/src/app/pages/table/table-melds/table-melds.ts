import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Fit, MeldEnd, Swap } from '../../../core/cards/card';
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
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-melds.html',
  styleUrl: './table-melds.scss',
})
export class TableMelds {
  readonly melds = input<readonly MeldView[]>([]);

  /**
   * Where the card the player has picked out could join what is lying here.
   * Shown whosever turn it is: knowing that a card has a place is worth
   * knowing while you wait for your turn to use it.
   */
  readonly fits = input<readonly Fit[]>([]);

  /**
   * The jokers lying here that the picked card could buy out. Shown the
   * same way and for the same reason: that a card has a joker waiting for
   * it is worth knowing before it is your turn to pay for it.
   */
  readonly buys = input<readonly Swap[]>([]);

  /** Whether pressing one of those places would do anything. */
  readonly offered = input(false);

  readonly chose = output<Fit>();

  readonly bought = output<Swap>();

  protected readonly picture = pictureOf;

  /** Whether the picked card could join that meld at that end. */
  protected fitsAt(meld: number, end: MeldEnd): boolean {
    return this.fits().some((fit) => fit.meld === meld && fit.end === end);
  }

  /** Whether the card lying there is a joker the picked card could buy. */
  protected buyable(meld: number, at: number): boolean {
    return this.buys().some((swap) => swap.meld === meld && swap.at === at);
  }

  /** Whether pressing it would buy it, which also needs it to be their turn. */
  protected forSale(meld: number, at: number): boolean {
    return this.offered() && this.buyable(meld, at);
  }

  protected buy(meld: number, at: number): void {
    if (this.forSale(meld, at)) {
      this.bought.emit({ meld, at });
    }
  }

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
