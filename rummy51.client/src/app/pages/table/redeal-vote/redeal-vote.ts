import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/** One seat and what it has said, as this player is allowed to see it. */
export interface VoteRow {
  readonly name: string;
  readonly answer: boolean | null;
}

/**
 * Rules 12.3 and 12.4. The masking is the server's - what may not be shown
 * never arrives - so a null means "not known to you", and a spinner or a
 * question mark is whether this player has answered.
 */
@Component({
  selector: 'app-redeal-vote',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './redeal-vote.html',
  styleUrl: './redeal-vote.scss',
})
export class RedealVote {
  readonly by = input('');

  readonly rows = input<readonly VoteRow[]>([]);

  /** This player has still to answer, which is what keeps the rest hidden. */
  readonly waiting = input(false);

  /** Everybody has answered, so the result stands and may be read. */
  readonly settled = input(false);

  readonly left = input(0);

  readonly answered = output<boolean>();

  readonly closed = output<void>();
}
