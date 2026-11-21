import { Signal, computed, signal } from '@angular/core';
import { HandView, LobbyTable, TableSeat } from '../../core/lobby/lobby.models';
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
  readonly across = computed(() => this.table()?.mode === 'survival');

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
    const rows = hand.scores.map((score, seat) => ({
      place: hand.places[seat],
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

    return this.across() ? byStanding(rows) : rows.sort((one, other) => one.place - other.place);
  });

  /**
   * Rule 9.4 in the middle of a hand, where there is no hand to show yet.
   * How close somebody is to the number is how the hand in front of them
   * should be played, so it is worth asking for before it is over.
   */
  readonly standingRows = computed<readonly ResultRow[]>(() => {
    const hand = this.hand();
    const seats = this.seats();

    if (!hand || !this.across()) {
      return [];
    }

    const { totals, hands, last, out } = hand.standing;
    const played = Math.max(...hands);

    return byStanding(
      seats.flatMap((who, seat) =>
        who.playerId
          ? [
              {
                place: 0,
                name: who.displayName ?? '',
                score: last[seat] ?? 0,
                won: false,
                wentDown: false,
                mine: seat === this.ownSeat(),
                total: totals[seat] ?? 0,
                hands: hands[seat] ?? 0,
                out: out[seat] ?? false,
                playedLast: (hands[seat] ?? 0) === played,
                staying: null,
              },
            ]
          : [],
      ),
    );
  });

  /** Open only while somebody is reading it. */
  readonly open = signal(false);

  /**
   * Whether the result window has been put away. It says which result was
   * put away rather than that one was, because everything about a table
   * arrives again every time anybody else moves: a flag would be cleared
   * by somebody else pressing their own button, and the window would come
   * back in front of whoever had just closed it.
   */
  readonly done = computed(
    () => this.resultNumber() > 0 && this.readAway() === this.resultNumber(),
  );

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

  /** Rule 9.5: who the game went to, once there is a game to have been won. */
  readonly gameWinner = computed(() => {
    const seat = this.hand()?.standing.won ?? -1;

    return seat === -1 ? null : (this.seats()[seat]?.displayName ?? '');
  });

  readonly wonTheGame = computed(() => (this.hand()?.standing.won ?? -1) === this.ownSeat());

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
    this.readAway.set(this.resultNumber());
    this.moves.readTheHand();
  }

  /** Rule 9.6. */
  wentOn(leaving: boolean): void {
    if (!leaving) {
      this.readAway.set(this.resultNumber());

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

  /**
   * Which result is on the screen, counted in the hands behind it, and
   * nought before there is one.
   */
  private readonly resultNumber = computed(() => {
    const hands = this.hand()?.standing.hands ?? [];

    return hands.length > 0 ? Math.max(...hands) : 0;
  });

  /** The last result this player read and put away. */
  private readonly readAway = signal(0);
}

/**
 * Rule 9.4: whoever is furthest from the number comes first, and whoever
 * is out of it sits under the rest whatever they have. The place is where
 * that order put them and not what any one hand came to.
 */
function byStanding(rows: readonly ResultRow[]): readonly ResultRow[] {
  return [...rows]
    .sort((one, other) => Number(one.out) - Number(other.out) || one.total - other.total)
    .map((row, at) => ({ ...row, place: at + 1 }));
}
