/**
 * A bar drawn over everything, marking the place something is going to.
 *
 * It is fixed to the window and painted above whatever is being carried,
 * and that is the whole of its design. A hand is laid overlapped, so where
 * a card will land is read from the gap the others have left for it - and
 * the card in your hand lies over that gap whenever you are travelling
 * towards it, which is to say whenever you most want to see it. A mark that
 * can be covered answers the easy direction only.
 */
export class Marker {
  private constructor(private readonly element: HTMLElement) {}

  static raise(): Marker {
    const bar = document.createElement('div');

    bar.setAttribute('inert', '');
    bar.style.position = 'fixed';
    // Window pixels, which have neither a start nor an end to speak of.
    bar.style.left = '0';
    bar.style.top = '0';
    bar.style.width = '3px';
    bar.style.borderRadius = '2px';
    bar.style.background = 'var(--felt-500)';
    bar.style.boxShadow = '0 0 6px 1px var(--felt-500)';
    bar.style.pointerEvents = 'none';
    // Above what is being carried, which rides at forty.
    bar.style.zIndex = '50';

    document.body.append(bar);

    return new Marker(bar);
  }

  /** Along the leading edge of the place, and as tall as it is. */
  over(place: DOMRect): void {
    this.element.style.height = `${place.height}px`;
    this.element.style.transform = `translate(${place.left - 1}px, ${place.top}px)`;
  }

  remove(): void {
    this.element.remove();
  }
}
