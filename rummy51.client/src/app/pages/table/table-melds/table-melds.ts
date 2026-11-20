import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterRenderEffect,
  computed,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Fit, MeldEnd, Swap } from '../../../core/cards/card';
import { pictureOf } from '../../../core/cards/deck';
import { MeldView } from '../../../core/lobby/lobby.models';

/** How wide a laid card is drawn, in rem, at most and at least. */
const Widest = 2.5;
const Narrowest = 1;

/**
 * Overflow worth acting on, in pixels. Rounding is what it is there to
 * forgive, and rounding is measured in pixels: a share of the board would
 * forgive three of them on a tall one, which is enough of a card's edge to
 * see under the buttons.
 */
const Slack = 2;

/**
 * The least a step may shrink by. The estimate below is allowed to be
 * timid and a timid step measures again, so without a floor a board that
 * spills by a hair would take dozens of layouts to give up that hair.
 */
const AtLeast = 0.9;

/**
 * A bound on the search rather than a part of it: giving up a tenth at
 * worst, the whole way from the widest to the narrowest is nine steps.
 */
const Steps = 12;

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

  private readonly board = viewChild.required<ElementRef<HTMLElement>>('board');

  private readonly scale = signal(Widest);

  protected readonly size = computed(() => `${this.scale()}rem`);

  constructor() {
    // The board does not grow to hold more: it is the screen, and the
    // screen is what it is. So what gives way as the table fills is the
    // cards.
    //
    // Reading what is lying here is what makes this run again when it
    // changes, and it is the only thing that does: measuring reads the
    // DOM rather than a signal, so an effect that only measured would
    // depend on nothing and run once, against an empty board.
    afterRenderEffect(() => {
      this.melds();
      this.fits();
      this.buys();

      untracked(() => this.scale.set(Narrowed(this.board().nativeElement)));
    });
  }

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
}

/**
 * How wide a laid card has to be for the melds to fit, starting at the
 * widest and narrowing until they do. Measured rather than worked out:
 * the melds wrap, so five short ones take more room than one long one,
 * and narrowing them packs more to a line - which is why an estimate from
 * the overflow undershoots and the step after it measures again.
 *
 * It runs the whole search in one go, writing each width it tries. So the
 * board already wears the answer by the time it is returned, and the
 * signal that holds it is told rather than asked.
 */
function Narrowed(board: HTMLElement): number {
  let laid = Widest;

  for (let step = 0; step < Steps; step++) {
    board.style.setProperty('--laid', `${laid}rem`);

    const over = Spilled(board);

    if (over <= 1 || laid <= Narrowest) {
      break;
    }

    laid = Math.max(Narrowest, Math.min(laid / Math.sqrt(over), laid * AtLeast));
  }

  return laid;
}

/**
 * How much more room the melds wanted than they were given. One or less is
 * a fit. A meld too wide to wrap spills sideways instead, so both are asked
 * and the worse of them answers.
 */
function Spilled(board: HTMLElement): number {
  return Math.max(
    Over(board.scrollHeight, board.clientHeight),
    Over(board.scrollWidth, board.clientWidth),
  );
}

/** One where it fits, and how many times over where it does not. */
function Over(wanted: number, given: number): number {
  return given > 0 && wanted - given > Slack ? wanted / given : 1;
}
