/**
 * Elements put in the air, and taken down together afterwards.
 *
 * A card travelling across the table has to leave the flow to do it: the
 * felt clips what leaves it, the fan scrolls inside that, and the seats are
 * outside both. Flown as it lies, a card would be cut off at the edge and
 * would raise a scrollbar on its way out. So a copy is fixed to the window
 * instead, in window pixels, which have neither a start nor an end to speak
 * of - and what it leaves behind keeps its place in the row.
 *
 * This holds the part every such journey shares. Where each one is going,
 * how long it takes and what it looks like on the way belong to whoever is
 * sending it.
 */
export class Flight {
  private readonly inAir: HTMLElement[] = [];

  /**
   * Fixed where it already lies, and read back before it is told anything
   * else. Without that reading the browser is given both ends of the
   * journey in one go, has nothing to move from, and arrives without
   * travelling.
   */
  send(element: HTMLElement, from: DOMRect): HTMLElement {
    element.style.position = 'fixed';
    element.style.left = `${from.left}px`;
    element.style.top = `${from.top}px`;
    element.style.width = `${from.width}px`;
    element.style.margin = '0';
    element.setAttribute('inert', '');

    document.body.append(element);
    this.inAir.push(element);
    element.getBoundingClientRect();

    return element;
  }

  /** The real cards are where they belong by now, so the copies can go. */
  landAll(): void {
    this.inAir.splice(0).forEach((element) => element.remove());
  }
}

/**
 * Centre to centre, because a card lying on its side does not have the box
 * an upright one has and matching their corners would set it adrift.
 */
export function betweenCentres(from: DOMRect, to: DOMRect): string {
  const across = to.left + to.width / 2 - from.left - from.width / 2;
  const down = to.top + to.height / 2 - from.top - from.height / 2;

  return `translate(${across}px, ${down}px)`;
}
