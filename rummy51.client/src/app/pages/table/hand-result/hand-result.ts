import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Doubling } from '../../../core/lobby/lobby.models';

/** One player as the hand left them, and as the game has left them. */
export interface ResultRow {
  readonly place: number;
  readonly name: string;
  readonly score: number;
  readonly won: boolean;
  readonly wentDown: boolean;
  readonly mine: boolean;

  /** Rules 9.4 and 9.5, and nought where the game is only this hand. */
  readonly total: number;
  readonly hands: number;
  readonly out: boolean;

  /**
   * Whether they were in the hand being shown. A seat knocked out by it was,
   * and has a score to show for it; one knocked out before it was not, and
   * an empty cell is the truth rather than a nought.
   */
  readonly playedLast: boolean;

  /** Rules 9.3 and 10.8: null until that seat has said. */
  readonly staying: boolean | null;
}

/**
 * How a hand came out, where the game stands after it, and the one question
 * the moment asks. All three are in one window because they are one moment:
 * the hand is over and nobody is deciding anything about cards.
 */
@Component({
  selector: 'app-hand-result',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hand-result.html',
  styleUrl: './hand-result.scss',
})
export class HandResult {
  readonly rows = input<readonly ResultRow[]>([]);

  readonly winner = input('');

  /** Rule 8.2, broken out, so the window can say what met which condition. */
  readonly doubling = input.required<Doubling>();

  /** Rule 9.2: this hand is one of several, so the game is worth its columns. */
  readonly across = input(false);

  /** Rule 9.3: this player has still to say whether they want another game. */
  readonly asking = input(false);

  /** Rule 9.6: this is the hand that put them out, and they have a choice. */
  readonly justOut = input(false);

  /** Rule 9.4: the game goes on, so what follows this window is a deal. */
  readonly anotherHand = input(false);

  /** Rule 9.5: who the game itself went to, once there is a game to have won. */
  readonly gameWinner = input<string | null>(null);

  /** Whether that is the one reading this. */
  readonly wonTheGame = input(false);

  readonly left = input(0);

  readonly stayed = output<boolean>();

  readonly wentOn = output<boolean>();

  /** Read, and done with: the window goes before the clock takes it. */
  readonly closed = output<void>();

  /**
   * The viewer's own line, or null for anybody watching a table they are
   * not sitting at. It is what turns the heading from news into an answer:
   * the hand is told to whoever took it, and to everybody else it is told
   * about somebody, with where they came underneath.
   */
  protected readonly me = computed(() => this.rows().find((row) => row.mine) ?? null);

  /**
   * How the hand was taken, which is also what doubled it where it did.
   * Said in every case and not only the uncommon ones: "an ordinary win"
   * is the answer to the same question and is worth as much as the others.
   */
  protected readonly how = computed(() => {
    const doubling = this.doubling();

    if (doubling.rummy) {
      return 'table.byRummy';
    }

    return doubling.finishingCard ? 'table.byFinishingCard' : 'table.ordinaryWin';
  });
}
