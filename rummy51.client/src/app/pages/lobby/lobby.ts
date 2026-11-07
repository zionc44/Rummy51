import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { OnlinePlayers } from './online-players/online-players';
import { SeatedNotice } from './seated-notice/seated-notice';
import { TableLists } from './table-lists/table-lists';

@Component({
  selector: 'app-lobby',
  imports: [TranslatePipe, PageShell, ProfileMenu, OnlinePlayers, SeatedNotice, TableLists],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './lobby.html',
  styleUrl: './lobby.scss',
})
export class Lobby {
  protected readonly auth = inject(AuthService);
  protected readonly lobby = inject(LobbyConnection);

  /**
   * Reconnecting displaces whichever connection took this player's place,
   * which is the same rule applied from this side. The connection itself
   * belongs to the session, not to this page, so nothing here opens or
   * closes it on the way in and out.
   */
  protected reconnect(): void {
    void this.lobby.start();
  }
}
