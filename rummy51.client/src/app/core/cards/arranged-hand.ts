import { Signal, computed, linkedSignal, signal } from '@angular/core';
import { HeldCard } from './card';
import { HandOrder, arrangedLike, inOrder, keepingTheirPlaces, moved } from './card-order';

/**
 * Where a hand wraps until its owner says otherwise. Eight is what leaves
 * each card enough of itself showing to be read on a phone.
 */
const PerRow = 8;

/**
 * A hand as its owner has chosen to hold it, which is the one thing about a
 * hand that the server never learns: it sends the cards and is never told an
 * order. Sorting, moving and picking a card out all land here.
 *
 * It is held against what the server sends rather than instead of it. A hand
 * changes under its owner all game - they draw, they throw - and what they
 * decided about the cards in between has to survive that. Which is why the
 * two things that could drift are worked out from the cards rather than
 * simply remembered: where the hand wraps, and which card is standing out
 * of it.
 */
export class ArrangedHand {
  /** Null until the hand has been sorted or a card moved by hand. */
  private readonly laid = signal<readonly HeldCard[] | null>(null);

  /** Which way the last press sorted them, so the next one does the other. */
  private sortedBy: HandOrder = 'bySuit';

  constructor(private readonly held: Signal<readonly HeldCard[]>) {}

  readonly cards = computed(() => arrangedLike(this.held(), this.laid()));

  /**
   * How many cards lie on the first line. A card never crosses between the
   * lines on its own, whatever happens to the hand: one thrown from the
   * first line takes its place with it rather than pulling a card up from
   * the line below, and one drawn joins the end. Only the player carrying a
   * card across moves the break, and that goes through `move`.
   */
  readonly firstLine = linkedSignal<readonly HeldCard[], number>({
    source: this.cards,
    computation: (cards, previous) =>
      previous === undefined || previous.source.length === 0
        ? Math.min(PerRow, cards.length)
        : Math.max(previous.value - gone(previous.source.slice(0, previous.value), cards), 0),
  });

  /**
   * The card standing clear of the rest, or null when none is. A card just
   * drawn arrives standing out, because it is the one its owner is thinking
   * about; one that leaves the hand stops standing out of it.
   */
  readonly picked = linkedSignal<readonly HeldCard[], number | null>({
    source: this.cards,
    computation: (cards, previous) => {
      if (previous === undefined) {
        return null;
      }

      const arrived = newcomers(previous.source, cards);

      // A hand that was already there and has gained exactly one card has
      // just drawn it. A hand gains fourteen at the deal and none of them.
      if (arrived.length === 1 && previous.source.length > 0) {
        return arrived[0].id;
      }

      return holds(cards, previous.value) ? previous.value : null;
    },
  });

  /**
   * Two presses and two orders, because a run and a set are not the same
   * shape: by rank the sets line up, by suit the runs do. Either of them
   * takes over from an arrangement made by hand, which is what asking to
   * have the hand sorted means.
   */
  sort(): void {
    this.sortedBy = this.sortedBy === 'byRank' ? 'bySuit' : 'byRank';

    // Everything, including what is lying on the table: a card laid down is
    // still one of this hand's, and sorting puts it where it belongs so
    // that it is already there if it comes back.
    this.laid.set(inOrder(this.everything(), this.sortedBy));
    this.firstLine.set(Math.min(PerRow, this.held().length));
  }

  /** One card, or a whole run, put down somewhere else in the hand. */
  move(ids: readonly number[], to: number, firstLine: number | null): void {
    // The place counts over the cards lying in the hand, so the move is made
    // in those terms and then folded back into the whole of the
    // arrangement, places kept and all.
    this.laid.set(keepingTheirPlaces(moved(this.cards(), ids, to), this.laid() ?? []));

    // Putting a card down somewhere drops the mark on it. The player has
    // said what they wanted with that card, and it was not "this one".
    this.picked.set(null);

    if (firstLine !== null) {
      this.firstLine.set(firstLine);
    }
  }

  /**
   * A tap on a card stands it out of the hand. The same card again puts it
   * back, and a different one takes its place: there is only ever one.
   */
  pick(id: number): void {
    this.picked.update((standing) => (standing === id ? null : id));
  }

  /**
   * Every card this hand has held, which is not the same as every card it
   * is holding: what has been laid on the table is still arranged.
   */
  private everything(): readonly HeldCard[] {
    const held = this.held();
    const here = new Set(held.map((card) => card.id));

    return [...held, ...(this.laid() ?? []).filter((card) => !here.has(card.id))];
  }

  /** A new hand. Whatever was decided about the last one goes with it. */
  forget(): void {
    this.laid.set(null);
    this.firstLine.set(0);
    this.picked.set(null);
  }
}

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
