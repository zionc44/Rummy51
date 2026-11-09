import { Place } from '../../../core/cards/hand-place';

/**
 * The hand as it is drawn, read back.
 *
 * A drag hit-tests rectangles, so where the cards are on screen is its
 * subject and not a copy of one. The hand already publishes everything that
 * takes: every place is a `.hand__card`, a card among them carries its own
 * id and the run or set it is lying in, and a place carrying neither is a
 * gap kept for a card in the air. This turns that back into data.
 */

const Line = '.hand__row';
const Card = '.hand__card';
const Band = 'hand__band';

export const idOf = (card: HTMLElement): number | null =>
  card.dataset['id'] === undefined ? null : Number(card.dataset['id']);

/** Every place in the hand, in the order it is laid. */
export function placesIn(hand: HTMLElement): HTMLElement[] {
  return [...hand.querySelectorAll<HTMLElement>(Card)];
}

/**
 * The same places as measured boxes, grouped into the lines they lie in.
 *
 * What is being carried is a gap here whatever the screen still shows. The
 * hand draws it as one too, but only at the next render, and the answer to
 * "where does this belong" cannot wait a frame for it - nor should it have
 * to ask: a card in the air is not lying anywhere, and whoever is carrying
 * it knows that before anybody has drawn it.
 */
export function laidIn(
  hand: HTMLElement,
  carried: readonly number[],
): readonly (readonly Place[])[] {
  const inAir = new Set(carried);

  return [...hand.querySelectorAll<HTMLElement>(Line)].map((line) =>
    [...line.querySelectorAll<HTMLElement>(Card)].map((card) => {
      const id = idOf(card);

      return { id: id !== null && inAir.has(id) ? null : id, box: card.getBoundingClientRect() };
    }),
  );
}

/** The cards themselves, found by the ids their places carry. */
export function cardsAmong(hand: HTMLElement, ids: readonly number[]): HTMLElement[] {
  const wanted = new Set(ids);

  return placesIn(hand).filter((card) => {
    const id = idOf(card);

    return id !== null && wanted.has(id);
  });
}

/** The first place with nothing in it: where what is carried will go. */
export function gapIn(hand: HTMLElement): HTMLElement | undefined {
  return placesIn(hand).find((card) => idOf(card) === null);
}

/**
 * What a press means. The band is the one part of a card that says "these
 * go together", so it takes them together; the face above it takes the one
 * card out of the run. Anywhere else takes nothing.
 */
export function takenBy(event: Event): readonly number[] {
  const pressed = event.target as HTMLElement | null;
  const card = pressed?.closest<HTMLElement>(Card) ?? null;

  if (pressed === null || card === null) {
    return [];
  }

  const held = pressed.classList.contains(Band) ? meldAround(card) : [idOf(card)];

  return held.filter((id): id is number => id !== null);
}

/** Every card lying in the same run or set, on the same line as this one. */
function meldAround(card: HTMLElement): readonly (number | null)[] {
  const meld = card.dataset['meld'];
  const line = card.parentElement;

  if (meld === undefined || line === null) {
    return [];
  }

  return [...line.querySelectorAll<HTMLElement>(Card)]
    .filter((other) => other.dataset['meld'] === meld)
    .map(idOf);
}
