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
import { meldsIn } from '../../../core/cards/melds';
import { placesIn } from './drawn-hand';
import { HandDrag } from './hand-drag';
import { slideFrom, whereTheyAre } from './hand-slide';

/**
 * The deck a hand is read from. It is held overlapped, so all that shows of
 * a card is the corner that names it, and that deck is drawn for exactly
 * this. Everywhere a card is seen whole reads from the other one.
 */
const Deck = 'cards/corners/';

/**
 * How many cards the first line holds until the player says otherwise. Eight
 * is what leaves each of them enough of itself showing to be read on a phone,
 * but where a hand wraps is the player's to decide: a run they have gathered
 * belongs on one line, however many cards it is.
 */
const PerRow = 8;

/** How many colours the bands run through before starting again. */
const Bands = 3;

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
  hostDirectives: [{ directive: HandDrag, inputs: ['arrangeable'], outputs: ['moved'] }],
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
   * How many cards the player keeps on the first line. Null until they have
   * moved one across, which is the only thing that changes it.
   */
  readonly firstLine = input<number | null>(null);

  private readonly drag = inject(HandDrag);

  protected readonly deck = Deck;

  private readonly oneRow = oneRowFits();

  /**
   * Fifteen cards across a phone leave twenty pixels of each one showing,
   * which is not enough to tell one from another. Two lines double that.
   */
  private readonly splitting = computed(() => !this.oneRow() && this.cards().length > 0);

  private readonly lineBreak = computed(() => {
    const asked = this.firstLine() ?? PerRow;
    const whole = Math.max(this.cards().length, this.places());

    // Each line keeps at least one card. An empty line has no card to
    // measure against, so the pointer could never be said to be over it
    // and nothing could ever be put back.
    return Math.min(Math.max(asked, 1), Math.max(whole - 1, 1));
  });

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

    return [laid.slice(0, breaks), laid.slice(breaks)];
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
  private readonly melds = computed(() => this.rows().map((row) => meldsIn(row)));

  /**
   * Which run or set a card is lying in, or null where it is in none. The
   * numbers only have to differ between neighbours, which is what lets one
   * meld be told from the next - and what lets a whole one be picked up.
   */
  protected meldAt(line: number, at: number): number | null {
    return this.melds()[line]?.[at] ?? null;
  }

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
}
