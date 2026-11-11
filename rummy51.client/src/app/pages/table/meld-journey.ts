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
 * is going. It remembers where every card on the screen was one render ago,
 * and when it is told which cards have crossed it sends each of them from
 * there to wherever it is now. A card that did not move is left alone,
 * which is how taking back a card off the discard pile passes through here
 * without doing anything.
 *
 * A card in somebody else's hand is drawn nowhere at all - a hand is a pile
 * of backs and a count - so for those the pile itself stands in at whichever
 * end cannot be seen. A card laid flies out of that pile face up, and one
 * taken back flies into it face down, which is exactly what happens to it.
 *
 * Copies make the journey while the cards themselves wait out of sight: the
 * felt clips what leaves it and the hand lies outside, so nothing can cross
 * between them in the flow.
 */
export class MeldJourney {
  private readonly flight = new Flight();
  private was = new Map<number, DOMRect>();
  private now = new Map<number, DOMRect>();
  private waiting: HTMLElement[] = [];
  private landing = 0;

  constructor(private readonly root: () => HTMLElement | undefined) {}

  /**
   * Where everything on the screen is, kept from one render to the next.
   * Nobody has to say in advance that cards are about to move: by the time
   * anybody knows they have, where they were is already here.
   */
  remember(): void {
    this.was = this.now;
    this.now = this.whereTheyAre();
  }

  /**
   * These cards have crossed between a hand and the table. Where they were
   * is already known; where they are going is wherever they are now, and
   * `unseen` is where a card nobody may look at is kept - the pile of backs
   * at the seat whose cards these are.
   */
  crossed(ids: readonly number[], unseen: HTMLElement | undefined): void {
    this.land();

    const hidden = unseen?.getBoundingClientRect();
    const drawn = this.cardsBy(ids);

    for (const id of ids) {
      const card = drawn.get(id);

      this.send(card ?? unseen, this.was.get(id) ?? hidden, card ? undefined : hidden);
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

  /**
   * One card on its way. What is flown is a copy of whatever is drawn at the
   * end it can be seen at, which for a card going into a hand is the pile it
   * is joining - a card going somewhere nobody may look at goes face down,
   * because that is what becomes of it.
   */
  private send(what: HTMLElement | undefined, from: DOMRect | undefined, into?: DOMRect): void {
    const to = into ?? what?.getBoundingClientRect();

    if (what === undefined || from === undefined || to === undefined) {
      return;
    }

    if (from.left === to.left && from.top === to.top) {
      return;
    }

    const copy = what.cloneNode(true) as HTMLElement;

    copy.style.zIndex = `${Above}`;
    this.flight.send(copy, from);
    copy.style.height = `${from.height}px`;
    copy.style.transition = `transform ${Travel}ms ease-in-out`;
    copy.style.transform = `${betweenCentres(from, to)} scale(${to.width / from.width})`;

    // Only the card itself waits out of sight. A pile standing in for one
    // is somewhere else's furniture and stays where it is.
    if (into === undefined) {
      what.style.visibility = 'hidden';
      this.waiting.push(what);
    }
  }

  private whereTheyAre(): Map<number, DOMRect> {
    const found = new Map<number, DOMRect>();

    for (const [id, card] of this.cardsBy(null)) {
      found.set(id, card.getBoundingClientRect());
    }

    return found;
  }

  /** Those cards as they are drawn now, wherever that may be - or all of them. */
  private cardsBy(ids: readonly number[] | null): Map<number, HTMLElement> {
    const root = this.root();
    const wanted = ids === null ? null : new Set(ids);
    const found = new Map<number, HTMLElement>();

    if (root === undefined) {
      return found;
    }

    for (const card of root.querySelectorAll<HTMLElement>('[data-id]')) {
      const id = Number(card.dataset['id']);

      if ((wanted === null || wanted.has(id)) && !found.has(id)) {
        found.set(id, card);
      }
    }

    return found;
  }
}
