import { Flight, betweenCentres } from '../../core/dom/flight';
import { DrawnFrom } from '../../core/lobby/lobby.models';

/** How long one card takes to cross between a pile and a hand. */
const Travel = 320;

/** Over the carried card and under the mark that says where it would land. */
const Above = 45;

/** The two piles on the felt and the hand below them, as a view holds them. */
export interface Piles {
  stock(): HTMLElement | undefined;
  discards(): HTMLElement | undefined;
  cardAt(id: number): HTMLElement | undefined;
}

/**
 * One card seen to travel between a pile and a hand. Without it a card
 * drawn leaves one place and appears in another in the same instant, and
 * nobody at the table can tell what moved - least of all the players
 * watching somebody else's turn, for whom the whole of the move is a count
 * going up by one.
 *
 * A copy makes the journey while the card itself waits out of sight, because
 * the hand is laid inside a box that clips and the piles are outside it.
 */
export class CardJourney {
  private readonly flight = new Flight();
  private leftFrom: DOMRect | null = null;
  private waiting: HTMLElement | null = null;
  private landing = 0;

  constructor(private readonly piles: Piles) {}

  /**
   * Where a card being drawn is setting out from, read now: by the time it
   * reaches the hand the pile may be showing another card, or none.
   */
  leaving(pile: DrawnFrom): void {
    const from = pile === 'stock' ? this.piles.stock() : this.piles.discards();

    this.leftFrom = from?.getBoundingClientRect() ?? null;
  }

  /** And the card itself, once it is lying where it is going to lie. */
  arrived(cardId: number | null): void {
    const from = this.leftFrom;
    const card = cardId === null ? undefined : this.piles.cardAt(cardId);

    this.leftFrom = null;

    if (from !== null && card !== undefined) {
      this.fly(card, from, card.getBoundingClientRect());
    }
  }

  /**
   * A card thrown, which waits for nothing: it is still lying in the hand
   * and the pile it is going to is already where it will be.
   */
  thrown(cardId: number): void {
    const card = this.piles.cardAt(cardId);
    const pile = this.piles.discards()?.getBoundingClientRect();

    if (card !== undefined && pile !== undefined) {
      this.fly(card, card.getBoundingClientRect(), pile);
    }
  }

  /** The copy away and the card itself back, in that order and once only. */
  land(): void {
    window.clearTimeout(this.landing);
    this.landing = 0;

    this.flight.landAll();

    if (this.waiting !== null) {
      this.waiting.style.visibility = '';
      this.waiting = null;
    }
  }

  private fly(card: HTMLElement, from: DOMRect, to: DOMRect): void {
    // Whoever turned animation off in their system meant this too: the card
    // simply is where it has gone.
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return;
    }

    // Anything still on its way from the move before is put down at once,
    // so there are never two of them in the air.
    this.land();

    const copy = this.flight.send(card.cloneNode(true) as HTMLElement, from);

    // The height as well as the width, because a card in a hand is given
    // one by the line it lies in and would otherwise keep that one.
    copy.style.height = `${from.height}px`;
    copy.style.zIndex = `${Above}`;
    copy.style.transition = `transform ${Travel}ms ease-in-out`;

    card.style.visibility = 'hidden';
    this.waiting = card;

    requestAnimationFrame(() => {
      copy.style.transform = `${betweenCentres(from, to)} scale(${to.width / from.width})`;
    });

    this.landing = window.setTimeout(() => this.land(), Travel);
  }
}
