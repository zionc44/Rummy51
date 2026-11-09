import { idOf, placesIn } from './drawn-hand';

/**
 * Cards travelling to where they have been sent rather than appearing
 * there, so a hand rearranges itself in front of the player rather than at
 * them.
 *
 * It is done by measuring where every card is after a render, putting the
 * ones that have moved back where they were, and letting them go from
 * there. The two halves belong to different moments and are kept apart for
 * that reason: whoever calls them must read before the frame is drawn and
 * write after, and must never hand back a reading from an older render.
 */

/** How long a card takes to travel to where sorting has sent it. */
const Slide = 300;

/** Where every card in the hand is now, by which card it is. */
export function whereTheyAre(hand: HTMLElement): ReadonlyMap<number, DOMRect> {
  const found = new Map<number, DOMRect>();

  for (const card of placesIn(hand)) {
    const id = idOf(card);

    if (id !== null) {
      found.set(id, card.getBoundingClientRect());
    }
  }

  return found;
}

/** A card that has not moved is left alone, which is most of them. */
export function slideFrom(
  hand: HTMLElement,
  before: ReadonlyMap<number, DOMRect>,
  now: ReadonlyMap<number, DOMRect>,
): void {
  for (const card of placesIn(hand)) {
    const id = idOf(card);

    if (id === null) {
      continue;
    }

    const from = before.get(id);
    const to = now.get(id);

    if (from === undefined || to === undefined) {
      continue;
    }

    const across = from.left - to.left;
    const down = from.top - to.top;

    if (across === 0 && down === 0) {
      continue;
    }

    card.style.transition = 'none';
    card.style.transform = `translate(${across}px, ${down}px)`;

    // A frame to be drawn where it was, or it is given both ends of the
    // journey at once and arrives without travelling.
    requestAnimationFrame(() => {
      card.style.transition = `transform ${Slide}ms ease-out`;
      card.style.transform = '';
    });
  }
}
