import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Doubling } from '../../../core/lobby/lobby.models';

/** One player as the hand left them, placed under rule 8.3. */
export interface ResultRow {
  readonly place: number;
  readonly name: string;
  readonly score: number;
  readonly won: boolean;
  readonly wentDown: boolean;
  readonly mine: boolean;

  /** Rules 9.3 and 10.8: null until that seat has said. */
  readonly staying: boolean | null;
}

/**
 * How a hand came out, and the one question a finished game asks. Both are
 * in the same window because they are the same moment: the table is over
 * and nobody is deciding anything about cards any more.
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

  /** Rule 9.3: this player has still to say whether they want another. */
  readonly asking = input(false);

  readonly left = input(0);

  readonly stayed = output<boolean>();

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
