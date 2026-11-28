import { Signal, computed, signal } from '@angular/core';
import { HandPhase, HandView, LobbyTable, TableSeat } from '../../core/lobby/lobby.models';
import { ScorePad, scorePadOf } from '../../core/lobby/score-pad';
import { ResultRow } from './hand-result/hand-result';
import { Moves } from './moves';

/**
 * How the hand came out, where the game stands after it, and the one
 * question that moment asks.
 *
 * The twin of `Asking`. That one is chapter 12's question put to this
 * player; this is chapter 9's, and both of them read their own window and
 * answer it, so that the page does neither.
 */
export class Reckoning {
  constructor(
    private readonly hand: Signal<HandView | null>,
    private readonly table: Signal<LobbyTable | null>,
    private readonly seats: Signal<readonly TableSeat[]>,
    private readonly ownSeat: Signal<number>,
    private readonly moves: Moves,
  ) {}

  /** Rule 9.2: a game of several hands, so where it stands is worth a column. */
  readonly across = computed(() => {
    const mode = this.table()?.mode;

    return mode !== undefined && mode !== 'single';
  });

  /** What the standing is read against: rule 9.4's number, or how far 9.7's hands have gone. */
  readonly progress = computed((): Progress | null => {
    const table = this.table();
    const hand = this.hand();

    if (!table || table.limit === null || !hand) {
      return null;
    }

    if (table.mode === 'survival') {
      return { key: 'lobby.points', params: { '0': table.limit } };
    }

    const counted = hand.standing.eachHand.length;
    const now = hand.phase === 'finished' ? counted : counted + 1;

    return { key: 'table.handOf', params: { '0': Math.min(now, table.limit), '1': table.limit } };
  });

  readonly winnerName = computed(() => {
    const hand = this.hand();

    return hand ? (this.seats()[hand.winnerSeat]?.displayName ?? '') : '';
  });

  /**
   * Rules 8.3 and 9.4. A hand on its own is placed by what it came to; a
   * hand that is one of several is placed by where the game stands after
   * it, which is the question the next hand will be played on. Whoever is
   * out of it sits under the rest whatever they have.
   */
  readonly results = computed<readonly ResultRow[]>(() => {
    const hand = this.hand();
    const seats = this.seats();

    if (!hand?.scores.length) {
      return [];
    }

    const standing = hand.standing;
    const played = Math.max(...standing.hands);
    // Rule 8.3 places a hand and rule 9.5 places a game, and a game of
    // several is read for where it stands rather than for what its last
    // hand came to. Both come from the table: a placing worked out twice
    // is a placing that will be changed once.
    const rows = hand.scores.map((score, seat) => ({
      place: (this.across() ? standing.places[seat] : hand.places[seat]) ?? 0,
      name: seats[seat]?.displayName ?? '',
      score,
      won: seat === hand.winnerSeat,
      wentDown: hand.wentDown[seat],
      mine: seat === this.ownSeat(),
      total: standing.totals[seat] ?? 0,
      hands: standing.hands[seat] ?? 0,
      out: standing.out[seat] ?? false,
      playedLast: (standing.hands[seat] ?? 0) === played,
      staying: hand.staying[seat] ?? null,
    }));

    return [...rows].sort(byPlace);
  });

  /**
   * Rule 9.2: the game hand by hand, which is the standing as well as the
   * history of it - the bottom of a column is where that player stands.
   */
  readonly pad = computed<ScorePad>(() => {
    const hand = this.hand();

    return hand && this.across()
      ? scorePadOf(hand.standing, this.seats(), this.ownSeat(), hand.staying)
      : { columns: [], rows: [] };
  });

  /** Open only while somebody is reading it. */
  readonly open = signal(false);

  /**
   * Whether the result window has been put away. The table is asked rather
   * than remembered - a seat says for itself whether it has read the hand,
   * and the table puts that back when the next one is dealt and again when
   * a game begins. Nothing here counts anything, so there is nothing left
   * counting from the game before.
   */
  readonly done = computed(() => {
    const hand = this.hand();
    const seat = this.ownSeat();

    if (hand === null) {
      return false;
    }

    // Somebody watching a table they are not sitting at is in none of its
    // arrays and never will be, so what they have put away is their own.
    if (seat === -1) {
      return this.putAway();
    }

    return hand.standing.read[seat] === true || (hand.staying[seat] ?? null) !== null;
  });

  /** A window put away is open again the moment there is another result. */
  settle(phase: HandPhase | null): void {
    if (phase !== 'finished') {
      this.putAway.set(false);
    }
  }

  /**
   * Rule 9.6: out of the game, and this is the hand that did it. Asked once
   * and not again - a later hand finds them with fewer behind them than
   * whoever is still playing.
   */
  readonly justOut = computed(() => {
    const hand = this.hand();
    const seat = this.ownSeat();

    if (!hand?.scores.length || seat === -1 || hand.staying.length > 0) {
      return false;
    }

    const { out, hands } = hand.standing;

    return out[seat] === true && hands[seat] === Math.max(...hands);
  });

  /** Rule 9.4: nobody is being asked anything, so a deal is what follows. */
  readonly anotherHand = computed(() => (this.hand()?.staying.length ?? 0) === 0);

  /** Rules 9.5 and 9.8, less the viewer: a win they share is told to them as theirs. */
  readonly othersWhoWon = computed(() =>
    (this.hand()?.standing.won ?? [])
      .filter((seat) => seat !== this.ownSeat())
      .map((seat) => this.seats()[seat]?.displayName ?? ''),
  );

  readonly wonTheGame = computed(() =>
    (this.hand()?.standing.won ?? []).includes(this.ownSeat()),
  );

  /** Rule 9.3: the table is asking and this player has still to answer. */
  readonly askedToStay = computed(() => {
    const staying = this.hand()?.staying ?? [];
    const seat = this.ownSeat();

    return seat !== -1 && staying.length > 0 && staying[seat] === null;
  });

  /**
   * Read, and ready for the next. The window goes at once for whoever said
   * so; the table deals as soon as everybody watching has.
   */
  readTheHand(): void {
    this.putAway.set(true);
    this.moves.readTheHand();
  }

  /** Rule 9.6, where staying to watch is reading it and nothing more. */
  wentOn(leaving: boolean): void {
    if (!leaving) {
      this.readTheHand();

      return;
    }

    this.moves.giveUpTheChair();
    this.moves.backToLobby();
  }

  /** Rule 9.3. */
  stay(another: boolean): void {
    this.moves.stay(another);

    // Rule 10.8 frees the chair once everybody has answered, which can be
    // a while yet. Nobody who has given it up should sit watching for it.
    if (!another) {
      this.moves.backToLobby();
    }
  }

  /** The one thing here a watcher has to be remembered by, and only them. */
  private readonly putAway = signal(false);
}

/** A line for the standing's title, already chosen and ready to translate. */
export interface Progress {
  readonly key: string;
  readonly params: Readonly<Record<string, number>>;
}

/** Nought is no place at all, so it comes last rather than ahead of first. */
function byPlace(one: ResultRow, other: ResultRow): number {
  return (one.place || Infinity) - (other.place || Infinity);
}
