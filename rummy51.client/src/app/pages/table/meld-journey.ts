import { Flight, betweenCentres } from '../../core/dom/flight';

/** How long a card takes to cross between a hand and the table. */
const Travel = 320;

/** Over the felt and the hand both, since it passes across the two. */
const Above = 45;

/**
 * Cards seen to cross between a hand and the table - laid down, or taken
 * back again. Without it a run leaves a hand and appears on the felt in the
 * same instant, and the players watching somebody else's turn see three
 * cards they have never been shown arrive from nowhere.
 *
 * One journey serves both directions, because it is not told which way it
 * is going. It is told which cards to watch and where they are now; after
 * the table has been told and drawn again, it finds those same cards
 * wherever they have ended up and sends them from one to the other. A card
 * that did not move is left alone, which is how taking back a card off the
 * discard pile passes through here without doing anything.
 *
 * Copies make the journey while the cards themselves wait out of sight: the
 * felt clips what leaves it and the hand lies outside, so nothing can cross
 * between them in the flow.
 */
export class MeldJourney {
  private readonly flight = new Flight();
  private leftFrom = new Map<number, DOMRect>();
  private waiting: HTMLElement[] = [];
  private landing = 0;

  constructor(private readonly root: () => HTMLElement | undefined) {}

  /** Where these cards are, read now: in a moment they will be elsewhere. */
  leaving(ids: readonly number[]): void {
    this.leftFrom = new Map(
      [...this.cardsBy(ids)].map(([id, card]) => [id, card.getBoundingClientRect()]),
    );
  }

  /** And where they have landed, once the table has been drawn again. */
  arrived(): void {
    const before = this.leftFrom;
    this.leftFrom = new Map();

    if (before.size === 0) {
      return;
    }

    this.land();

    for (const [id, card] of this.cardsBy([...before.keys()])) {
      this.send(card, before.get(id));
    }

    if (this.waiting.length > 0) {
      this.landing = window.setTimeout(() => this.land(), Travel);
    }
  }

  /** The copies come down and the cards they stood in for come back. */
  land(): void {
    window.clearTimeout(this.landing);
    this.landing = 0;

    this.flight.landAll();
    this.waiting.splice(0).forEach((card) => (card.style.visibility = ''));
  }

  private send(card: HTMLElement, from: DOMRect | undefined): void {
    const to = card.getBoundingClientRect();

    if (from === undefined || (from.left === to.left && from.top === to.top)) {
      return;
    }

    const copy = card.cloneNode(true) as HTMLElement;

    copy.style.zIndex = `${Above}`;
    this.flight.send(copy, from);
    copy.style.transition = `transform ${Travel}ms ease-in-out`;
    copy.style.transform = `${betweenCentres(from, to)} scale(${to.width / from.width})`;

    card.style.visibility = 'hidden';
    this.waiting.push(card);
  }

  /** Every one of those cards as it is drawn now, wherever that may be. */
  private cardsBy(ids: readonly number[]): Map<number, HTMLElement> {
    const root = this.root();
    const wanted = new Set(ids);
    const found = new Map<number, HTMLElement>();

    if (root === undefined) {
      return found;
    }

    for (const card of root.querySelectorAll<HTMLElement>('[data-id]')) {
      const id = Number(card.dataset['id']);

      if (wanted.has(id) && !found.has(id)) {
        found.set(id, card);
      }
    }

    return found;
  }
}
