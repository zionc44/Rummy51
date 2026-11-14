import { Signal, computed, linkedSignal, signal } from '@angular/core';
import { HeldCard } from './card';
import { HandOrder, arrangedLike, inOrder, keepingTheirPlaces, moved } from './card-order';

/** Eight leaves each card enough of itself showing to read on a phone. */
const PerRow = 8;

/**
 * A hand as its owner has chosen to hold it, which is the one thing about a
 * hand the server never learns: it sends the cards and is never told an
 * order. Sorting, moving and picking a card out all land here.
 */
export class ArrangedHand {
  /** Null until the hand has been sorted or a card moved by hand. */
  private readonly laid = signal<readonly HeldCard[] | null>(null);

  /** Which way the last press sorted them, so the next one does the other. */
  private sortedBy: HandOrder = 'bySuit';

  constructor(private readonly held: Signal<readonly HeldCard[]>) {}

  readonly cards = computed(() => arrangedLike(this.held(), this.laid()));

  /**
   * How many cards lie on the first line. Only the player carrying a card
   * across moves the break, so it travels one way on its own: up, as cards
   * leave. A redeal is on the screen throughout and passes through three
   * cards, so a break kept across one would be a break of one, and every
   * card of the hand that follows would land on the second line.
   */
  readonly firstLine = linkedSignal<readonly HeldCard[], number>({
    source: this.cards,
    computation: (cards, previous) =>
      previous === undefined || handedSeveral(previous.source, cards)
        ? Math.min(PerRow, cards.length)
        : Math.max(previous.value - gone(previous.source.slice(0, previous.value), cards), 0),
  });

  /** The card standing clear of the rest, or null when none is. */
  readonly picked = linkedSignal<readonly HeldCard[], number | null>({
    source: this.cards,
    computation: (cards, previous) => {
      if (previous === undefined) {
        return null;
      }

      const arrived = newcomers(previous.source, cards);

      // One card into a hand that was already there is a card drawn, and
      // the deal is fourteen into a hand that was not.
      if (arrived.length === 1 && previous.source.length > 0) {
        return arrived[0].id;
      }

      return holds(cards, previous.value) ? previous.value : null;
    },
  });

  /** Rule 4.1 names two shapes and no order shows both. */
  sort(): void {
    this.sortedBy = this.sortedBy === 'byRank' ? 'bySuit' : 'byRank';

    // What is lying on the table is sorted with the rest, so that a card
    // bought back or taken back arrives where it belongs.
    this.laid.set(inOrder(this.everything(), this.sortedBy));
    this.firstLine.set(Math.min(PerRow, this.held().length));
  }

  /** One card, or a whole run, put down somewhere else in the hand. */
  move(ids: readonly number[], to: number, firstLine: number | null): void {
    // The place counts over the cards in the hand, so the move is made in
    // those terms and folded back into the arrangement afterwards.
    this.laid.set(keepingTheirPlaces(moved(this.cards(), ids, to), this.laid() ?? []));

    this.picked.set(null);

    if (firstLine !== null) {
      this.firstLine.set(firstLine);
    }
  }

  /** The same card again puts it back; there is only ever one standing out. */
  pick(id: number): void {
    this.picked.update((standing) => (standing === id ? null : id));
  }

  /** Pointing at a card rather than tapping it, so pointing twice is not a toggle. */
  singleOut(id: number): void {
    this.picked.set(id);
  }

  /** Held, not holding: what was laid on the table is still arranged. */
  private everything(): readonly HeldCard[] {
    const held = this.held();
    const here = new Set(held.map((card) => card.id));

    return [...held, ...(this.laid() ?? []).filter((card) => !here.has(card.id))];
  }

  /** A new hand. Whatever was decided about the last one goes with it. */
  forget(): void {
    this.laid.set(null);
    this.firstLine.set(PerRow);
    this.picked.set(null);
  }
}

/**
 * More at once than a turn can bring, so the hand was dealt rather than
 * played into - a turn taken back counts too. Either way the break left
 * behind was decided about a hand that is gone.
 */
const handedSeveral = (were: readonly HeldCard[], cards: readonly HeldCard[]) =>
  cards.length > were.length + 1;

/** How many of those are no longer in the hand. */
function gone(were: readonly HeldCard[], cards: readonly HeldCard[]): number {
  const held = new Set(cards.map((card) => card.id));

  return were.filter((card) => !held.has(card.id)).length;
}

/** The cards in the hand now that were not in it before. */
function newcomers(were: readonly HeldCard[], cards: readonly HeldCard[]): readonly HeldCard[] {
  const before = new Set(were.map((card) => card.id));

  return cards.filter((card) => !before.has(card.id));
}

const holds = (cards: readonly HeldCard[], id: number | null) =>
  id !== null && cards.some((card) => card.id === id);
