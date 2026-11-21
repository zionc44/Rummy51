import { Signal, computed, signal } from '@angular/core';
import { HandPhase, HandView, Redeal, TableSeat } from '../../core/lobby/lobby.models';
import { Moves } from './moves';
import { VoteRow } from './redeal-vote/redeal-vote';

/**
 * What the table is asking this player: a redeal put to them under chapter
 * 12, the answers they have earned the right to see, the three cards they
 * are choosing, and rule 3.5's one question that is not a question at all.
 *
 * The twin of `Turn`, which says what they may do. This says what they are
 * being waited for, and both exist so that the screen can ask one question
 * rather than reading a phase and working it out. Answering is here as
 * well, because every answer puts a window away as well as sending
 * something, and the putting away is this screen's own.
 */
export class Asking {
  constructor(
    private readonly hand: Signal<HandView | null>,
    private readonly seats: Signal<readonly TableSeat[]>,
    private readonly ownSeat: Signal<number>,
    private readonly redeal: Signal<Redeal>,
    private readonly ticking: Signal<number>,
    private readonly moves: Moves,
  ) {}

  /** Who offered the redeal, by name, or empty while nobody has. */
  readonly by = computed(() => {
    const at = this.hand()?.offeredBy ?? -1;

    return at === -1 ? '' : (this.seats()[at]?.displayName ?? '');
  });

  /**
   * Rule 12.4, already masked by the server: a null is a seat this player
   * may not be shown.
   */
  readonly rows = computed<readonly VoteRow[]>(() => {
    const answers = this.redeal().answers;

    return this.seats()
      .map((seat, at) => ({ name: seat.displayName ?? '', answer: answers[at] ?? null }));
  });

  /** Still to answer, which is what keeps the other seats hidden. */
  readonly toAnswer = computed(() => (this.redeal().answers[this.ownSeat()] ?? null) === null);

  /** Rule 12.9: this player's own four pairs, and empty is no right to force. */
  readonly pairs = computed(() => this.redeal().pairs);

  /** Rule 12.9: what is on the table is a forced redeal and not an offer. */
  readonly forced = computed(() => this.hand()?.forced ?? false);

  /** Rule 12.11: the pairs it was forced with, once the table may see them. */
  readonly forcedWith = computed(() => this.hand()?.forcedWith ?? []);

  /**
   * Rule 12.11, worked out from the answers rather than asked for: one seat
   * choosing to end it is the one thing that changes what anybody keeps.
   * Forcing at once is every answer refused, so it lands here as well.
   */
  readonly ended = computed(() => this.rows().some((row) => row.answer === false));

  /** However long the table is being given, counted down rather than asked for again. */
  readonly secondsLeft = computed(() => {
    const closes = this.closesAt();

    return closes === null ? 0 : Math.max(0, Math.ceil((closes - this.ticking()) / 1000));
  });

  /**
   * Read and chosen are this screen's own, and are put back by `settle`
   * rather than linked to the phase. A signal that is only ever read while
   * its window is up is not evaluated while it is down, so a link would
   * carry the last answer into the next offer and shut the window at once.
   */
  readonly read = signal(false);

  readonly chosen = signal(false);

  /** Rule 12.10 being put to whoever may force, before anything is sent. */
  readonly choosing = signal(false);

  /**
   * Rule 3.5: the stock has gone and the pile is about to be it. Nobody is
   * being asked to agree, only to have seen it, so this is gone the moment
   * they say so and not when the table settles.
   */
  readonly theStockHasGone = computed(() => this.hand()?.phase === 'restocking');

  /**
   * And this player has said so, which leaves them watching for the rest.
   * Somebody watching a table they do not sit at is never waited for, so
   * there is nothing for them to say either.
   */
  readonly seenTheRestock = computed(() => {
    const seat = this.ownSeat();

    return seat === -1 || this.hand()?.seenTheRestock[seat] === true;
  });

  /** Rule 3.5, seen. */
  seeTheRestock(): void {
    this.moves.seeTheRestock();
  }

  /** Rule 12.9, which only opens the choice of what to force. */
  chooseHowToForce(): void {
    this.choosing.set(true);
  }

  /** Rule 12.10. */
  forceARedeal(atOnce: boolean): void {
    this.choosing.set(false);
    this.moves.forceARedeal(atOnce);
  }

  /** Rule 12.4. */
  readTheResult(): void {
    this.read.set(true);
    this.moves.readTheResult();
  }

  /** Rule 12.6. */
  keep(cardIds: readonly number[]): void {
    this.chosen.set(true);
    this.moves.keep(cardIds);
  }

  /** A window answered once is open again the moment the table asks anew. */
  settle(phase: HandPhase | null): void {
    if (phase !== 'answered') {
      this.read.set(false);
    }

    if (phase !== 'keeping') {
      this.chosen.set(false);
    }

    if (phase !== 'playing') {
      this.choosing.set(false);
    }
  }

  /**
   * When the wait the table was last told about runs out, as a moment. A
   * message says how much is left of it as it was sent; a countdown needs
   * to know where the end is.
   */
  private readonly closesAt = computed(() => {
    const waiting = this.hand()?.waiting ?? null;

    return waiting === null ? null : Date.now() + waiting.secondsLeft * 1000;
  });
}
