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
  output,
  signal,
  viewChildren,
} from '@angular/core';
import { HeldCard } from '../../../core/cards/card';
import { meldsIn } from '../../../core/cards/melds';

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

/** How long a card takes to travel to where sorting has sent it. */
const Slide = 300;

/** How far a finger travels before it is a drag and not a tap. */
const Grip = 6;

/** How many colours the bands run through before starting again. */
const Bands = 3;

/** How far above the finger the card rides, so it is not hidden by it. */
const Lift = 32;

/** Cards picked up and put down somewhere else in the same hand. */
export interface CardMove {
  readonly ids: readonly number[];
  readonly to: number;

  /**
   * How many cards the first line holds once it has landed, or null where
   * the hand is on one line and there is no second for it to cross into.
   */
  readonly firstLine: number | null;
}

/** What is being carried, and where it was taken hold of. */
interface Grasp {
  readonly ids: readonly number[];
  readonly from: { readonly x: number; readonly y: number };
  readonly grabbedAt: { readonly x: number; readonly y: number };
  readonly width: number;
  readonly height: number;
}

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
 * The player's own cards, laid as they have chosen to hold them. It shows a
 * hand, says where a card dragged out of it should go, and knows nothing
 * about whose hand it is or what the game makes of it.
 */
@Component({
  selector: 'app-player-hand',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(pointermove)': 'drag($event)',
    '(pointerup)': 'drop()',
    '(pointercancel)': 'drop()',
  },
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

  /**
   * Whether these cards may be picked up and put down somewhere else. Only
   * the player holding them may: anybody else looking at this place is
   * watching, and what they would rearrange is not theirs.
   */
  readonly arrangeable = input(false);

  readonly moved = output<CardMove>();

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
    const lifted = this.dragging();

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

  /** The colour of the band under a card, or null where it is in no meld. */
  protected bandOf(line: number, at: number): string | null {
    const meld = this.melds()[line]?.[at];

    return meld === undefined || meld === null ? null : `var(--meld-${(meld % Bands) + 1})`;
  }

  /** A card is known by which card it is; an empty place, by where it is. */
  protected keyOf(card: HeldCard | null, at: number): string {
    return card === null ? `place ${at}` : `card ${card.id}`;
  }

  /** The cards in the player's hand rather than lying in it, if any are. */
  protected readonly dragging = signal<readonly number[]>([]);

  private readonly landings = viewChildren<ElementRef<HTMLElement>>('landing');

  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);
  private grasp: Grasp | null = null;
  private carried: HTMLElement | null = null;
  private landing = 0;
  private lastPlace = '';
  private seen: ReadonlyMap<string, DOMRect> = new Map();

  constructor() {
    // Where every card is, read after each render. One the sort has moved
    // is then put back where it was and let travel from there, so a hand
    // rearranges itself in front of the player rather than at them.
    afterRenderEffect({
      earlyRead: () => {
        this.cards();

        const before = this.seen;
        this.seen = this.whereTheCardsAre();

        return before;
      },
      write: (before) => this.slideFrom(before()),
    });

    // A card left in the air when the page goes is a card left in the body.
    inject(DestroyRef).onDestroy(() => this.settle());
  }

  /** Where a card flying to this hand comes to rest, place by place. */
  landingSpot(step: number): HTMLElement | undefined {
    return this.landings()[step]?.nativeElement;
  }

  /** One card, taken by its face. */
  protected take(event: PointerEvent, card: HeldCard): void {
    this.begin(event, [card.id]);
  }

  /**
   * A whole run or set, taken by the band that marks it. The band is the
   * one part of a card that says "these go together", so it is the part
   * that carries them together.
   */
  protected takeMeld(event: PointerEvent, line: number, at: number): void {
    const meld = this.melds()[line]?.[at];

    if (meld === undefined || meld === null) {
      return;
    }

    const ids = (this.rows()[line] ?? [])
      .map((card, place) => (this.melds()[line]?.[place] === meld ? card?.id : undefined))
      .filter((id): id is number => id !== undefined);

    this.begin(event, ids);
  }

  /**
   * Taken, but not yet dragged. A finger that comes down and goes up again
   * without travelling is a tap, and taps belong to the game rather than to
   * the arranging of a hand.
   */
  private begin(event: PointerEvent, ids: readonly number[]): void {
    if (this.grasp !== null || ids.length === 0 || !this.arrangeable()) {
      return;
    }

    // Anything still on its way down from the move before is put down at
    // once, so there are never two of them in the air.
    this.settle();

    // Otherwise the browser takes a picture to be its own to drag, which
    // is why this worked with a finger and not with a mouse.
    event.preventDefault();

    const boxes = this.elementsFor(ids).map((element) => element.getBoundingClientRect());

    if (boxes.length === 0) {
      return;
    }

    const left = Math.min(...boxes.map((box) => box.left));
    const top = Math.min(...boxes.map((box) => box.top));

    this.host.nativeElement.setPointerCapture(event.pointerId);
    this.grasp = {
      ids,
      from: { x: left, y: top },
      grabbedAt: { x: event.clientX - left, y: event.clientY - top },
      width: Math.max(...boxes.map((box) => box.right)) - left,
      height: Math.max(...boxes.map((box) => box.bottom)) - top,
    };
  }

  /** The cards themselves, found by the names their places carry. */
  private elementsFor(ids: readonly number[]): HTMLElement[] {
    const wanted = new Set(ids.map((id) => `card ${id}`));

    return this.landings()
      .map((ref) => ref.nativeElement)
      .filter((element) => wanted.has(element.dataset['place'] ?? ''));
  }

  protected drag(event: PointerEvent): void {
    const grasp = this.grasp;

    if (grasp === null) {
      return;
    }

    if (this.dragging().length === 0) {
      const travelled = Math.hypot(
        event.clientX - grasp.from.x - grasp.grabbedAt.x,
        event.clientY - grasp.from.y - grasp.grabbedAt.y,
      );

      if (travelled < Grip) {
        return;
      }

      this.lift(grasp);
    }

    this.follow(event, grasp);

    const place = this.placeFor(event.clientX, event.clientY);
    const to = place.line === 0 ? place.within : place.inFirst + place.within;

    // Crossing between the lines is what moves the break, and nothing else
    // does: eight cards above stay eight, and nine stay nine.
    const firstLine = !this.splitting()
      ? null
      : place.line === 0
        ? place.inFirst + grasp.ids.length
        : place.inFirst;

    const move = `${to} of ${firstLine}`;

    if (move !== this.lastPlace) {
      this.lastPlace = move;
      this.moved.emit({ ids: grasp.ids, to, firstLine });
    }
  }

  /**
   * Put down. The card travels from the finger to the gap that has been
   * kept for it rather than appearing there, and only then is the gap
   * filled, so there is never both a card and a copy of it.
   */
  protected drop(): void {
    const carried = this.carried;
    this.grasp = null;
    this.lastPlace = '';

    if (carried === null) {
      return;
    }

    const gap = this.gap()?.getBoundingClientRect();

    if (gap === undefined) {
      this.settle();
      return;
    }

    carried.style.transition = `transform ${Slide}ms ease-out`;
    carried.style.transform = `translate(${gap.left}px, ${gap.top}px)`;
    this.landing = window.setTimeout(() => this.settle(), Slide);
  }

  /** The gap is filled and the copy is gone, in that order and once only. */
  private settle(): void {
    window.clearTimeout(this.landing);
    this.landing = 0;

    this.carried?.remove();
    this.carried = null;
    this.dragging.set([]);
  }

  /**
   * A copy fixed to the window, because the hand is inside a box that
   * clips. A run is copied whole, each card where it was lying, so what
   * travels looks like what was picked up.
   */
  private lift(grasp: Grasp): void {
    const carried = document.createElement('div');

    carried.setAttribute('inert', '');
    carried.style.position = 'fixed';
    // Window pixels, which have neither a start nor an end to speak of.
    carried.style.left = '0';
    carried.style.top = '0';
    carried.style.zIndex = '40';
    carried.style.pointerEvents = 'none';
    carried.style.width = `${grasp.width}px`;
    carried.style.height = `${grasp.height}px`;
    carried.style.transform = `translate(${grasp.from.x}px, ${grasp.from.y}px)`;

    this.elementsFor(grasp.ids).forEach((element, at) => {
      const box = element.getBoundingClientRect();
      const copy = element.cloneNode(true) as HTMLElement;

      copy.style.position = 'absolute';
      copy.style.left = `${box.left - grasp.from.x}px`;
      copy.style.top = `${box.top - grasp.from.y}px`;
      copy.style.width = `${box.width}px`;
      copy.style.height = `${box.height}px`;
      copy.style.margin = '0';
      copy.style.zIndex = `${at}`;
      carried.append(copy);
    });

    document.body.append(carried);
    this.carried = carried;
    this.dragging.set(grasp.ids);
  }

  private follow(event: PointerEvent, grasp: Grasp): void {
    const carried = this.carried;

    if (carried === null) {
      return;
    }

    const x = event.clientX - grasp.grabbedAt.x;
    const y = event.clientY - grasp.grabbedAt.y - Lift;

    carried.style.transform = `translate(${x}px, ${y}px) scale(1.1)`;
  }

  /** The first place in the hand with nothing in it: where they will go. */
  private gap(): HTMLElement | undefined {
    return this.landings()
      .map((ref) => ref.nativeElement)
      .find((element) => element.dataset['place'] === undefined);
  }

  /**
   * Where the pointer says the card belongs: which line, how many of the
   * cards on that line it comes after, and how many are on the first -
   * which is what turns a place on a line into a place in the hand.
   */
  private placeFor(x: number, y: number): { line: number; within: number; inFirst: number } {
    const rows = this.rows();
    const boxes = this.landings().map((ref) => ref.nativeElement.getBoundingClientRect());
    const line = this.lineUnder(y, rows, boxes);

    let within = 0;
    let inFirst = 0;
    let at = 0;

    for (const [index, cards] of rows.entries()) {
      for (const card of cards) {
        const box = boxes[at++];

        if (card === null || box === undefined) {
          continue;
        }

        if (index === 0) {
          inFirst++;
        }

        if (index === line && box.left + box.width / 2 < x) {
          within++;
        }
      }
    }

    return { line, within, inFirst };
  }

  private lineUnder(
    y: number,
    rows: readonly (readonly (HeldCard | null)[])[],
    boxes: readonly DOMRect[],
  ): number {
    let at = 0;
    let nearest = Number.POSITIVE_INFINITY;
    let line = 0;

    for (const [index, cards] of rows.entries()) {
      const box = boxes[at];
      at += cards.length;

      if (box === undefined) {
        continue;
      }

      const away = Math.abs(y - (box.top + box.height / 2));

      if (away < nearest) {
        nearest = away;
        line = index;
      }
    }

    return line;
  }

  private whereTheCardsAre(): ReadonlyMap<string, DOMRect> {
    const found = new Map<string, DOMRect>();

    for (const card of this.landings()) {
      const place = card.nativeElement.dataset['place'];

      if (place !== undefined) {
        found.set(place, card.nativeElement.getBoundingClientRect());
      }
    }

    return found;
  }

  /** A card that has not moved is left alone, which is most of them. */
  private slideFrom(before: ReadonlyMap<string, DOMRect>): void {
    // Not while a card is being dragged. A card part way through a slide
    // measures where it appears rather than where it belongs, and the place
    // the finger is over is worked out from those measurements - so sliding
    // during a drag feeds itself and the hand never settles.
    if (this.dragging().length > 0) {
      return;
    }

    for (const card of this.landings()) {
      const element = card.nativeElement;
      const place = element.dataset['place'] ?? '';
      const from = before.get(place);
      const to = this.seen.get(place);

      if (from === undefined || to === undefined) {
        continue;
      }

      const across = from.left - to.left;
      const down = from.top - to.top;

      if (across === 0 && down === 0) {
        continue;
      }

      element.style.transition = 'none';
      element.style.transform = `translate(${across}px, ${down}px)`;

      // A frame to be drawn where it was, or it is given both ends of the
      // journey at once and arrives without travelling.
      requestAnimationFrame(() => {
        element.style.transition = `transform ${Slide}ms ease-out`;
        element.style.transform = '';
      });
    }
  }
}
