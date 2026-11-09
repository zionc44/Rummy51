import { WritableSignal, signal } from '@angular/core';

/**
 * How many cards have reached each seat, and how many are still on their
 * way there.
 *
 * A card in the air has its place kept for it, so the row it is joining is
 * already the width it will be and nothing shifts as it lands. That is the
 * whole reason the two are counted apart.
 */
export class Arrivals {
  private readonly landed = signal<readonly number[]>([]);
  private readonly flying = signal<readonly number[]>([]);

  /** Nothing anywhere, which is where every hand starts. */
  forget(): void {
    this.landed.set([]);
    this.flying.set([]);
  }

  /** Places kept at a seat for cards about to be sent to it. */
  expect(seat: number, count: number): void {
    this.bump(this.flying, seat, count);
  }

  /** They are there, so they are no longer on their way. */
  land(seat: number, count: number): void {
    this.bump(this.flying, seat, -count);
    this.bump(this.landed, seat, count);
  }

  /** How many have reached that seat. */
  reached(seat: number): number {
    return at(this.landed(), seat);
  }

  /** How many are reaching it: the ones there and the ones still coming. */
  reaching(seat: number): number {
    return this.reached(seat) + at(this.flying(), seat);
  }

  private bump(counts: WritableSignal<readonly number[]>, seat: number, by: number): void {
    counts.update((current) => {
      const next = [...current];

      while (next.length <= seat) {
        next.push(0);
      }

      next[seat] += by;

      return next;
    });
  }
}

/** A seat nothing has reached yet is not in the list at all. */
const at = (counts: readonly number[], seat: number): number => counts[seat] ?? 0;
