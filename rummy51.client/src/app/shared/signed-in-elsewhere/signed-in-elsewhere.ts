import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LobbyConnection } from '../../core/lobby/lobby-connection';

/**
 * One player holds one connection, so opening the game somewhere else takes
 * it from here. It sits over the whole app rather than on a page, because
 * the connection is what every page is drawn from: a table left without one
 * is a table that has stopped saying anything, and a blank screen is a worse
 * answer than the truth.
 */
@Component({
  selector: 'app-signed-in-elsewhere',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './signed-in-elsewhere.html',
  styleUrl: './signed-in-elsewhere.scss',
})
export class SignedInElsewhere {
  private readonly lobby = inject(LobbyConnection);

  protected readonly elsewhere = this.lobby.signedInElsewhere;

  protected reconnect(): void {
    void this.lobby.start();
  }
}
