/**
 * Elements lifted out of where they are drawn and carried across the window.
 *
 * A hand is laid inside a box that clips, and the seats are outside it, so
 * anything travelling between them has to leave the flow: copied, fixed to
 * the window and moved in window pixels, which have neither a start nor an
 * end to speak of. What it leaves behind keeps its place in the line.
 */

export interface Point {
  readonly x: number;
  readonly y: number;
}

export class Carried {
  private constructor(private readonly element: HTMLElement) {}

  /**
   * A copy of each element, laid where it was lying, inside one box that
   * moves as a whole - so a run picked up by its band travels looking like
   * what was picked up rather than like a stack.
   */
  static of(cards: readonly HTMLElement[], origin: Point, width: number, height: number): Carried {
    const box = document.createElement('div');

    box.setAttribute('inert', '');
    box.style.position = 'fixed';
    box.style.left = '0';
    box.style.top = '0';
    box.style.zIndex = '40';
    box.style.pointerEvents = 'none';
    box.style.width = `${width}px`;
    box.style.height = `${height}px`;
    box.style.transform = `translate(${origin.x}px, ${origin.y}px)`;

    cards.forEach((card, at) => {
      const was = card.getBoundingClientRect();
      const copy = card.cloneNode(true) as HTMLElement;

      copy.style.position = 'absolute';
      copy.style.left = `${was.left - origin.x}px`;
      copy.style.top = `${was.top - origin.y}px`;
      copy.style.width = `${was.width}px`;
      copy.style.height = `${was.height}px`;
      copy.style.margin = '0';
      copy.style.zIndex = `${at}`;
      box.append(copy);
    });

    document.body.append(box);

    return new Carried(box);
  }

  /** Straight there, which is what following a finger is. */
  moveTo(point: Point, scale = 1): void {
    this.element.style.transition = '';
    this.element.style.transform = `translate(${point.x}px, ${point.y}px) scale(${scale})`;
  }

  /** Seen through, for when what lies under it matters as much. */
  fade(to: number): void {
    this.element.style.opacity = `${to}`;
  }

  /** Travelling there, which is what being let go is. */
  glideTo(point: Point, over: number): void {
    this.element.style.transition = `transform ${over}ms ease-out`;
    this.element.style.transform = `translate(${point.x}px, ${point.y}px)`;
  }

  remove(): void {
    this.element.remove();
  }
}
