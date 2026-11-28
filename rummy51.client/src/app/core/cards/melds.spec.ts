import { describe, expect, it } from 'vitest';
import { Ace, King } from './card';
import { C, D, H, S, joker } from './card.spec';
import {
  GoingDown,
  Least,
  hasCleanRun,
  isMeld,
  isRun,
  isSet,
  meldsIn,
  showingWorth,
  worthOnTable,
} from './melds';

/**
 * The twin of `Melding.cs`. Every answer here is one the server gives to
 * the same cards, which is what makes the copy checkable rather than a
 * promise - and where the two disagree, the server is right.
 */
describe('a set', () => {
  it('rule 4.1: three of a rank in different suits', () => {
    expect(isSet([S(9), H(9), D(9)])).toBe(true);
  });

  it('rule 4.1: four is the most there can be', () => {
    expect(isSet([S(9), H(9), D(9), C(9)])).toBe(true);
    expect(isSet([S(9), H(9), D(9), C(9), joker()])).toBe(false);
  });

  it('rule 4.1: and the same suit twice is not one', () => {
    expect(isSet([S(9), S(9), H(9)])).toBe(false);
  });

  it('rule 4.1: a joker takes a suit nobody has brought', () => {
    expect(isSet([S(9), H(9), joker()])).toBe(true);
  });

  it('rule 1.3: jokers alone name no rank, so they are no set', () => {
    expect(isSet([joker(1), joker(2), joker(1)])).toBe(false);
  });
});

describe('a run', () => {
  it('rule 4.1: three of a suit in order', () => {
    expect(isRun([S(4), S(5), S(6)])).toBe(true);
  });

  it('rule 4.1: read either way round, because a hand is laid as it is laid', () => {
    expect(isRun([S(6), S(5), S(4)])).toBe(true);
  });

  it('rule 4.1: and never across suits', () => {
    expect(isRun([S(4), H(5), S(6)])).toBe(false);
  });

  it('rule 4.1: a joker stands in whatever the place asks for', () => {
    expect(isRun([S(4), joker(), S(6)])).toBe(true);
    expect(isRun([joker(), S(5), S(6)])).toBe(true);
  });

  it('rule 4.2: the ace sits above the king', () => {
    expect(isRun([H(12), H(King), H(Ace)])).toBe(true);
  });

  it('rule 4.2: and below the two', () => {
    expect(isRun([H(Ace), H(2), H(3)])).toBe(true);
  });

  it('rule 4.2: but never turns the corner', () => {
    expect(isRun([H(King), H(Ace), H(2)])).toBe(false);
  });

  /** Rule 1.1 puts two of every card in the deck, so this is two aces. */
  it('rule 4.3: one run reaches from one ace to the other', () => {
    expect(isRun(aceToAce())).toBe(true);
  });

  it('rule 4.3: and either of them may be a joker', () => {
    expect(isRun([...aceToAce().slice(0, -1), joker()])).toBe(true);
  });

  it('rule 4.3: but fourteen places are all there are', () => {
    expect(isRun([...aceToAce(), H(2)])).toBe(false);
  });

  /** Rule 4.1: one place each, and these two are asking for one. */
  it('rule 4.3: and two aces beside each other are not two places', () => {
    expect(isRun([H(Ace), H(Ace), H(2)])).toBe(false);
  });

  /** Rule 1.2 and rule 5.3: the ace is worth eleven at both ends. */
  it('rule 4.3: and both of them are counted', () => {
    expect(worthOnTable(aceToAce())).toBe(11 + 54 + 30 + 11);
  });
});

/** A-2-3-...-K-A, which is the whole suit and one card over. */
const aceToAce = () => [H(Ace), ...Array.from({ length: 12 }, (_, at) => H(at + 2)), H(Ace)];

describe('a meld', () => {
  it('rule 4.1: is a run or a set and nothing else', () => {
    expect(isMeld([S(4), S(5), S(6)])).toBe(true);
    expect(isMeld([S(9), H(9), D(9)])).toBe(true);
    expect(isMeld([S(4), H(9), C(King)])).toBe(false);
  });

  it('rule 4.1: and never fewer than three cards', () => {
    expect(isMeld([S(4), S(5)])).toBe(false);
    expect(isMeld([S(9), H(9)])).toBe(false);
    expect(Least).toBe(3);
  });
});

describe('what a meld is worth on the table', () => {
  it('rule 5.3: a set is its rank as many times as there are cards', () => {
    expect(worthOnTable([S(9), H(9), D(9)])).toBe(27);
    expect(worthOnTable([S(9), H(9), D(9), C(9)])).toBe(36);
  });

  it('rule 5.5: a run is the places it covers', () => {
    expect(worthOnTable([S(4), S(5), S(6)])).toBe(15);
  });

  it('rule 1.2: an ace is eleven at either end of a run', () => {
    expect(worthOnTable([H(12), H(King), H(Ace)])).toBe(10 + 10 + 11);
    expect(worthOnTable([H(Ace), H(2), H(3)])).toBe(11 + 2 + 3);
  });

  it('rule 1.3: a joker is worth the place it is standing in', () => {
    expect(worthOnTable([S(4), joker(), S(6)])).toBe(15);
    expect(worthOnTable([S(9), H(9), joker()])).toBe(27);
  });

  it('rule 4.1: and what is not a meld is worth nothing', () => {
    expect(worthOnTable([S(4), H(9), C(King)])).toBe(0);
    expect(worthOnTable([S(4), S(5)])).toBe(0);
  });
});

describe('a clean run', () => {
  it('rule 5.2: three in a row with no joker among them', () => {
    expect(hasCleanRun([S(4), S(5), S(6)])).toBe(true);
  });

  it('rule 5.2: a joker in the middle leaves two and two', () => {
    expect(hasCleanRun([S(4), S(5), joker(), S(7)])).toBe(false);
  });

  it('rule 5.2: at the end it leaves three, which is enough', () => {
    expect(hasCleanRun([S(4), S(5), S(6), joker()])).toBe(true);
  });

  it('rule 5.2: and a set is never one, however clean', () => {
    expect(hasCleanRun([S(9), H(9), D(9)])).toBe(false);
  });
});

describe('what a turn has shown', () => {
  // Rule 5.3's own examples, which are the only worked ones the book has.
  const sixToTen = [S(6), S(7), S(8), joker(), S(10)];
  const fiveToNine = [H(5), H(6), H(7), joker(), H(9)];
  const threeTens = [S(10), H(10), D(10)];
  const cleanThree = [D(4), D(5), D(6)];

  it('rule 5.3: only the clean part of the providing run counts', () => {
    // Forty and thirty laid together, and the run has to provide the
    // clean three because a set can never be anybody's.
    expect(worthOnTable(sixToTen)).toBe(40);
    expect(worthOnTable(threeTens)).toBe(30);
    expect(showingWorth([sixToTen, threeTens])).toBe(51);
  });

  it('rule 5.3: laid alone, such a run is worth the clean three of it', () => {
    expect(worthOnTable(fiveToNine)).toBe(35);
    expect(showingWorth([fiveToNine])).toBe(18);
  });

  it('rule 5.4: beside a clean run of its own it is worth all of itself', () => {
    // The clean three costs nothing to give up, so it does the providing
    // and the broken run keeps its thirty-five.
    expect(showingWorth([fiveToNine, cleanThree])).toBe(35 + 15);
  });

  it('rule 5.4: a run with a joker and no clean three never provides', () => {
    const never = [C(9), C(10), joker(), C(12)];

    expect(hasCleanRun(never)).toBe(false);
    expect(worthOnTable(never)).toBe(39);
    expect(showingWorth([never, cleanThree])).toBe(39 + 15);
  });

  it('rule 5.5: nothing is ever taken off a set', () => {
    expect(worthOnTable([S(7), H(7), joker()])).toBe(21);
    expect(showingWorth([threeTens])).toBe(30);
  });

  it('rule 5.1: fifty-one is the number', () => {
    expect(GoingDown).toBe(51);
  });
});

describe('the runs and sets a line is lying in', () => {
  it('rule 4.1: a band for each, numbered apart from its neighbour', () => {
    const marks = meldsIn([S(4), S(5), S(6), H(9), D(9), C(9)]);

    expect(marks.slice(0, 3)).toEqual([0, 0, 0]);
    expect(marks.slice(3)).toEqual([1, 1, 1]);
  });

  it('rule 4.1: five in a row is one band and not two threes', () => {
    expect(meldsIn([S(4), S(5), S(6), S(7), S(8)])).toEqual([0, 0, 0, 0, 0]);
  });

  it('cards beside nothing belong to nothing', () => {
    expect(meldsIn([S(4), H(9), C(King)])).toEqual([null, null, null]);
  });

  it('a place still waiting for its card breaks the band', () => {
    expect(meldsIn([S(4), null, S(6)])).toEqual([null, null, null]);
  });

  it('two cards of a run are no band, so the hand says nothing yet', () => {
    expect(meldsIn([S(4), S(5)])).toEqual([null, null]);
  });
});
