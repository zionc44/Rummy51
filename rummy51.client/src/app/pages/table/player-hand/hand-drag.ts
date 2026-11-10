import { Directive, ElementRef, DestroyRef, inject, input, output, signal } from '@angular/core';
import { placeFor } from '../../../core/cards/hand-place';
import { Carried } from '../../../core/dom/carried';
import { Marker } from '../../../core/dom/marker';
import { cardsAmong, gapIn, laidIn, takenBy } from './drawn-hand';

/** How far a finger travels before it is a drag and not a tap. */
const Grip = 6;

/** How far above the finger the card rides, so it is not hidden by it. */
const Lift = 32;

/** How long the carried cards take to reach the gap kept for them. */
const Slide = 300;

/** Cards picked up and put down somewhere else in the same hand. */
export interface CardMove {
  readonly ids: readonly number[];
  readonly to: number;
  readonly firstLine: number | null;
}

/**
 * Cards let go of past the hand's own edge, and where. Inside the hand that
 * means a place; outside it, it might mean something to the table, and what
 * is lying out there is the table's to know and not the hand's.
 */
export interface CardRelease {
  readonly ids: readonly number[];
  readonly at: { readonly x: number; readonly y: number };
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
 * Carrying cards from one place in a hand to another: taking hold, the
 * travelling copy that follows the finger, and where letting go would put
 * them. What is on screen it asks `drawn-hand` for; where a place belongs
 * it asks `placeFor`; it decides the rest.
 */
@Directive({
  selector: '[appHandDrag]',
  host: {
    '(pointerdown)': 'take($event)',
    '(pointermove)': 'drag($event)',
    '(pointerup)': 'drop($event)',
    '(pointercancel)': 'drop()',
  },
})
export class HandDrag {
  /**
   * Whether these cards may be picked up at all. Only the player holding
   * them may: anybody else looking at this place is watching, and what they
   * would rearrange is not theirs.
   */
  readonly arrangeable = input(false);

  readonly moved = output<CardMove>();

  /**
   * One card touched and let go of without travelling, which is how a card
   * is singled out of a hand.
   */
  readonly tapped = output<number>();

  /**
   * A band touched and let go of without travelling. The band is the part
   * that says these cards go together, so dragging it carries them together
   * and tapping it plays them together - which is the one thing a run can
   * be asked to do that a card cannot.
   */
  readonly played = output<readonly number[]>();

  readonly released = output<CardRelease>();

  private readonly inAir = signal<readonly number[]>([]);

  /** The cards in the player's hand rather than lying in it, if any are. */
  readonly lifted = this.inAir.asReadonly();

  private readonly element: ElementRef<HTMLElement> = inject(ElementRef);

  private marker: Marker | null = null;

  /** The element the hand is drawn inside, which is this one. */
  private hand(): HTMLElement {
    return this.element.nativeElement;
  }

  private grasp: Grasp | null = null;
  private carried: Carried | null = null;
  private landing = 0;
  private lastPlace = '';

  constructor() {
    // A card left in the air when the page goes is a card left in the body.
    inject(DestroyRef).onDestroy(() => this.settle());
  }

  protected take(event: PointerEvent): void {
    this.begin(event, takenBy(event));
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

    const boxes = cardsAmong(this.hand(), ids).map((card) => card.getBoundingClientRect());

    if (boxes.length === 0) {
      return;
    }

    const left = Math.min(...boxes.map((box) => box.left));
    const top = Math.min(...boxes.map((box) => box.top));

    this.hand().setPointerCapture(event.pointerId);
    this.grasp = {
      ids,
      from: { x: left, y: top },
      grabbedAt: { x: event.clientX - left, y: event.clientY - top },
      width: Math.max(...boxes.map((box) => box.right)) - left,
      height: Math.max(...boxes.map((box) => box.bottom)) - top,
    };
  }

  protected drag(event: PointerEvent): void {
    const grasp = this.grasp;

    if (grasp === null) {
      return;
    }

    if (this.inAir().length === 0) {
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
    this.markLandingSpot();

    const landing = placeFor(
      { x: event.clientX, y: event.clientY },
      laidIn(this.hand(), grasp.ids),
      grasp.ids.length,
    );
    const move = `${landing.to} of ${landing.firstLine}`;

    if (move !== this.lastPlace) {
      this.lastPlace = move;
      this.moved.emit({ ids: grasp.ids, to: landing.to, firstLine: landing.firstLine });
    }
  }

  /**
   * Put down. The cards travel from the finger to the gap that has been
   * kept for them rather than appearing there, and only then is the gap
   * filled, so there is never both a card and a copy of it.
   */
  protected drop(event?: PointerEvent): void {
    const carried = this.carried;
    const grasp = this.grasp;
    this.grasp = null;
    this.lastPlace = '';

    if (carried === null) {
      // A finger lifted, and not a gesture the browser took away: a pointer
      // cancelled mid-press is not somebody choosing a card.
      if (event === undefined || grasp === null) {
        return;
      }

      if (grasp.ids.length === 1) {
        this.tapped.emit(grasp.ids[0]);
      } else {
        this.played.emit(grasp.ids);
      }

      return;
    }

    if (event !== undefined && grasp !== null) {
      this.tellWhereBeyondTheHand(event, grasp.ids);
    }

    const gap = gapIn(this.hand())?.getBoundingClientRect();

    if (gap === undefined) {
      this.settle();
      return;
    }

    carried.glideTo({ x: gap.left, y: gap.top }, Slide);
    this.landing = window.setTimeout(() => this.settle(), Slide);
  }

  /** The gap is filled and the copy is gone, in that order and once only. */
  private settle(): void {
    window.clearTimeout(this.landing);
    this.landing = 0;

    this.carried?.remove();
    this.carried = null;

    this.marker?.remove();
    this.marker = null;

    // Only when there is something to let go of. Setting it empty again is a
    // new array either way, and that is a change to everything reading it -
    // which is a whole hand re-rendered every time a card is merely pressed.
    if (this.inAir().length > 0) {
      this.inAir.set([]);
    }
  }

  private lift(grasp: Grasp): void {
    this.carried = Carried.of(
      cardsAmong(this.hand(), grasp.ids),
      grasp.from,
      grasp.width,
      grasp.height,
    );

    // Seen through, so the cards it lies over are still readable. It is in
    // your hand, not on the table, and it reads that way too.
    this.carried.fade(0.55);

    this.inAir.set(grasp.ids);
  }

  /** Riding above the finger rather than under it, where it can be seen. */
  private follow(event: PointerEvent, grasp: Grasp): void {
    this.carried?.moveTo(
      {
        x: event.clientX - grasp.grabbedAt.x,
        y: event.clientY - grasp.grabbedAt.y - Lift,
      },
      1.1,
    );
  }

  /**
   * The gap the others have left, marked over everything. The cards part
   * where this is going whichever way it travels, but what is in your hand
   * lies over that parting whenever you are moving towards it - which is
   * exactly when you want to see it. So the mark goes above.
   */
  private markLandingSpot(): void {
    const gap = gapIn(this.hand())?.getBoundingClientRect();

    if (gap === undefined) {
      return;
    }

    this.marker ??= Marker.raise();
    this.marker.over(gap);
  }

  /**
   * Let go of past the hand's own edge. Inside it, the cards are being
   * arranged and that is all this knows how to mean; outside it, somebody
   * else may have a use for the news.
   */
  private tellWhereBeyondTheHand(event: PointerEvent, ids: readonly number[]): void {
    const hand = this.hand().getBoundingClientRect();
    const at = { x: event.clientX, y: event.clientY };
    const inside =
      at.x >= hand.left && at.x <= hand.right && at.y >= hand.top && at.y <= hand.bottom;

    if (!inside) {
      this.released.emit({ ids, at });
    }
  }
}
