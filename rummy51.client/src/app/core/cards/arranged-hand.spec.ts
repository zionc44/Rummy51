import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';
import { ArrangedHand } from './arranged-hand';
import { HeldCard } from './card';
import { H, S } from './card.spec';

const codes = (cards: readonly HeldCard[]) => cards.map((card) => card.code);

/**
 * A hand the server keeps sending, which is how one reaches this.
 *
 * Both of the answers below are linked signals, and a linked signal has no
 * previous value until somebody has read it. So anything here that is about
 * what changed has to read it once before changing anything, exactly as the
 * screen does by drawing the hand.
 */
const dealt = (cards: readonly HeldCard[]) => {
  const held = signal<readonly HeldCard[]>(cards);
  const hand = new ArrangedHand(held);

  hand.cards();
  hand.picked();
  hand.firstLine();

  return { held, hand };
};

describe('a hand as its owner holds it', () => {
  it('is the hand as it arrived until they touch it', () => {
    const { hand } = dealt([S(9), H(2)]);

    expect(codes(hand.cards())).toEqual([S(9).code, H(2).code]);
  });

  it('one press lays it by rank and the next by suit', () => {
    // A two of spades and a nine of hearts come out the other way round
    // under the two orders, which is the whole reason there are two.
    const { hand } = dealt([S(2), H(9)]);

    hand.sort();
    const first = codes(hand.cards());

    hand.sort();

    expect(codes(hand.cards())).not.toEqual(first);

    hand.sort();

    expect(codes(hand.cards())).toEqual(first);
  });

  it('keeps the order across a card arriving', () => {
    const { held, hand } = dealt([S(9), H(2)]);

    hand.sort();

    const was = codes(hand.cards());
    const drawn = S(4);

    held.set([...held(), drawn]);

    expect(codes(hand.cards())).toEqual([...was, drawn.code]);
  });
});

describe('the card standing clear of the rest', () => {
  it('is nothing until one is picked', () => {
    const { hand } = dealt([S(9), H(2)]);

    expect(hand.picked()).toBeNull();
  });

  it('the same card again puts it back', () => {
    const cards = [S(9), H(2)];
    const { hand } = dealt(cards);

    hand.pick(cards[0].id);
    expect(hand.picked()).toBe(cards[0].id);

    hand.pick(cards[0].id);
    expect(hand.picked()).toBeNull();
  });

  it('pointing at one twice is not a toggle', () => {
    const cards = [S(9), H(2)];
    const { hand } = dealt(cards);

    hand.singleOut(cards[0].id);
    hand.singleOut(cards[0].id);

    expect(hand.picked()).toBe(cards[0].id);
  });

  it('rule 3.2: a card drawn is the card standing out the moment it lands', () => {
    const { held, hand } = dealt([S(9), H(2)]);
    const drawn = S(4);

    held.set([...held(), drawn]);

    expect(hand.picked()).toBe(drawn.id);
  });

  it('and a whole deal is not, there being nothing to have drawn', () => {
    const { held, hand } = dealt([]);

    held.set([S(2), S(3), S(4)]);

    expect(hand.picked()).toBeNull();
  });

  it('one that has left the hand stands out no longer', () => {
    const cards = [S(9), H(2)];
    const { held, hand } = dealt(cards);

    hand.pick(cards[0].id);
    held.set([cards[1]]);

    expect(hand.picked()).toBeNull();
  });
});

describe('where the hand wraps', () => {
  it('is eight, which leaves each card enough of itself to read', () => {
    const { hand } = dealt(Array.from({ length: 14 }, (_, at) => S(at + 2)));

    expect(hand.firstLine()).toBe(8);
  });

  it('a short hand wraps nowhere', () => {
    const { hand } = dealt([S(2), S(3)]);

    expect(hand.firstLine()).toBe(2);
  });

  it('only carrying a card across moves it, so a throw leaves it where it was', () => {
    const cards = Array.from({ length: 14 }, (_, at) => S(at + 2));
    const { held, hand } = dealt(cards);

    held.set(cards.slice(0, 13));

    expect(hand.firstLine()).toBe(8);
  });

  it('and a card leaving the first line takes the break down with it', () => {
    const cards = Array.from({ length: 14 }, (_, at) => S(at + 2));
    const { held, hand } = dealt(cards);

    held.set(cards.slice(1));

    expect(hand.firstLine()).toBe(7);
  });

  it('a new hand forgets all of it', () => {
    const cards = [S(2), S(3)];
    const { hand } = dealt(cards);

    hand.pick(cards[0].id);
    hand.forget();

    expect(hand.picked()).toBeNull();
    expect(hand.firstLine()).toBe(8);
  });
});
