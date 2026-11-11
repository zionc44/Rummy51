import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Signal,
  afterRenderEffect,
  computed,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { HeldCard } from '../../../core/cards/card';
import { pictureOf } from '../../../core/cards/deck';
import { meldsIn } from '../../../core/cards/melds';
import { cardsAmong, placesIn } from './drawn-hand';
import { HandDrag } from './hand-drag';
import { slideFrom, whereTheyAre } from './hand-slide';

/**
 * A line that has emptied still keeps one place, or there would be nowhere
 * left to carry a card back to.
 */
const orAPlace = (line: readonly (HeldCard | null)[]): readonly (HeldCard | null)[] =>
  line.length === 0 ? [null] : line;

/** How many colours the bands run through before starting again. */
const Bands = 5;

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
 * The player's own cards, laid as they have chosen to hold them: in one line
 * or two, with the runs and sets they are lying in banded where they lie.
 *
 * Picking a card up and putting it down elsewhere is `HandDrag`, which rides
 * on this element and reads the cards off it. Whoever uses this binds
 * `arrangeable` and listens to `moved` as if they were its own.
 */
@Component({
  selector: 'app-player-hand',
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [
    { directive: HandDrag, inputs: ['arrangeable'], outputs: ['moved', 'tapped', 'released', 'played'] },
  ],
  templateUrl: './player-hand.html',
  styleUrl: './player-hand.scss',
})
export class PlayerHand {
  /**
   * The cards in the player's hand. A null is one still on its way, drawn as
   * a gap, so the line is already the width it will be when the last one
   * lands.
   */
  readonly cards = input<readonly (HeldCard | null)[]>([]);

  /**
   * How many cards this hand holds when it is whole, which is more than are
   * in it while they are still being dealt.
   */
  readonly places = input(0);

  /**
   * The card the player has picked out, standing clear of the rest. Null
   * when none is. Only one ever stands out of a hand, which is what makes
   * it an answer to "this one" rather than a list.
   */
  readonly picked = input<number | null>(null);

  /**
   * How many cards the player keeps on the first line. Theirs, and never
   * worked out from how many cards there happen to be: a card crossing
   * between the lines on its own is the one thing a hand must never do.
   */
  readonly firstLine = input(0);

  private readonly drag = inject(HandDrag);

  protected readonly picture = pictureOf;

  private readonly oneRow = oneRowFits();

  /**
   * Fifteen cards across a phone leave twenty pixels of each one showing,
   * which is not enough to tell one from another. Two lines double that.
   */
  private readonly splitting = computed(() => !this.oneRow() && this.cards().length > 0);

  /** Clamped only so the break cannot fall outside the hand it breaks. */
  private readonly lineBreak = computed(() =>
    Math.min(this.firstLine(), Math.max(this.cards().length, this.places())),
  );

  /**
   * The hand as it is laid. Both lines are there from the first card, so the
   * ninth one arriving does not shift the eight that went before it.
   */
  protected readonly rows = computed<readonly (readonly (HeldCard | null)[])[]>(() => {
    const laid = this.asItIsHeld(this.splitting() ? this.withEveryPlace() : this.cards());

    if (!this.splitting()) {
      return [laid];
    }

    const breaks = this.lineBreak();

    return [orAPlace(laid.slice(0, breaks)), orAPlace(laid.slice(breaks))];
  });

  /** What is being dragged is drawn at the finger, so its places are gaps. */
  private asItIsHeld(cards: readonly (HeldCard | null)[]): readonly (HeldCard | null)[] {
    const lifted = this.drag.lifted();

    if (lifted.length === 0) {
      return cards;
    }

    return cards.map((card) => (card !== null && lifted.includes(card.id) ? null : card));
  }

  /**
   * The hand with a gap kept for every card still to come. A line that is
   * already its full width can be centred under the one above it without
   * sliding out from under the cards that have landed.
   */
  private withEveryPlace(): readonly (HeldCard | null)[] {
    const cards = this.cards();
    const missing = this.places() - cards.length;

    return missing > 0 ? [...cards, ...Array<HeldCard | null>(missing).fill(null)] : cards;
  }

  /**
   * Which cards are lying in a run or a set together, line by line. It is
   * read off the hand as it is laid, so a meld the player has not put side
   * by side is not one, and nor is one broken across the two lines.
   */
  private readonly melds = computed(() => {
    const lines: (number | null)[][] = [];
    let found = 0;

    // Numbered across the whole hand and not within each line: two melds on
    // two lines are two melds, and one colour between them says they are one.
    for (const row of this.rows()) {
      const marks = meldsIn(row);

      lines.push(marks.map((mark) => (mark === null ? null : mark + found)));
      found += new Set(marks.filter((mark) => mark !== null)).size;
    }

    return lines;
  });

  /**
   * Which run or set a card is lying in, or null where it is in none. The
   * number differs between every meld in the hand and not only between
   * neighbours, which is what lets one be told from the next, what lets a
   * whole one be picked up, and what gives each of them its own colour.
   */
  protected meldAt(line: number, at: number): number | null {
    return this.melds()[line]?.[at] ?? null;
  }

  /**
   * The runs and sets the hand is lying in, as the cards themselves. It is
   * read off the same laying the bands are, so what is counted is what is
   * banded: a meld broken across the two lines is not one, and a hand
   * nobody has arranged holds none.
   */
  readonly inMelds = computed<readonly (readonly HeldCard[])[]>(() => {
    const found = new Map<number, HeldCard[]>();

    this.rows().forEach((row, line) =>
      row.forEach((card, at) => {
        const meld = this.meldAt(line, at);

        if (card !== null && meld !== null) {
          found.set(meld, [...(found.get(meld) ?? []), card]);
        }
      }),
    );

    return [...found.values()];
  });

  /** The colour of the band under a card, or null where it is in no meld. */
  protected bandOf(line: number, at: number): string | null {
    const meld = this.meldAt(line, at);

    return meld === null ? null : `var(--meld-${(meld % Bands) + 1})`;
  }

  /** A card is known by which card it is; an empty place, by where it is. */
  protected keyOf(card: HeldCard | null, at: number): string {
    return card === null ? `place ${at}` : `card ${card.id}`;
  }

  private readonly element: ElementRef<HTMLElement> = inject(ElementRef);

  private seen: ReadonlyMap<number, DOMRect> = new Map();

  constructor() {
    // Read after each render and written to after the next, which is what
    // lets a card the sort has moved travel to its new place instead of
    // appearing in it. The rows are what is watched rather than the cards,
    // because they are what the laying out actually depends on - the cards,
    // where the player has broken the line, and the width there is.
    afterRenderEffect({
      earlyRead: () => {
        this.rows();

        const before = this.seen;
        this.seen = whereTheyAre(this.hand());

        return before;
      },
      // Untracked, and that is the whole of it. A measurement is only good
      // against the render it was taken from: what is read here - the rows,
      // whether a card is being dragged - would otherwise make this run
      // again on its own, holding a reading from the render before and
      // sliding the hand by a distance nothing has moved.
      write: (before) => {
        const was = before();

        untracked(() => this.slide(was));
      },
    });
  }

  private hand(): HTMLElement {
    return this.element.nativeElement;
  }

  /**
   * Not while a card is being dragged. A card part way through a slide
   * measures where it appears rather than where it belongs, and the place
   * the finger is over is worked out from those measurements - so sliding
   * during a drag feeds itself and the hand never settles.
   */
  private slide(before: ReadonlyMap<number, DOMRect>): void {
    if (this.drag.lifted().length > 0) {
      return;
    }

    slideFrom(this.hand(), before, this.seen);
  }

  /** Where a card flying to this hand comes to rest, place by place. */
  landingSpot(step: number): HTMLElement | undefined {
    return placesIn(this.hand())[step];
  }

  /** Where one card is lying in this hand, if it is lying in it at all. */
  cardAt(id: number): HTMLElement | undefined {
    return cardsAmong(this.hand(), [id])[0];
  }
}
