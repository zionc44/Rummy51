import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { OnlinePlayers } from './online-players/online-players';
import { TableLists } from './table-lists/table-lists';

@Component({
  selector: 'app-lobby',
  imports: [TranslatePipe, PageShell, ProfileMenu, OnlinePlayers, TableLists],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './lobby.html',
  styleUrl: './lobby.scss',
})
export class Lobby implements OnInit, OnDestroy {
  protected readonly auth = inject(AuthService);
  protected readonly lobby = inject(LobbyConnection);

  ngOnInit(): void {
    void this.lobby.start();
  }

  ngOnDestroy(): void {
    void this.lobby.stop();
  }

  /**
   * Reconnecting displaces whichever connection took this player's place, which
   * is the same rule applied from this side.
   */
  protected reconnect(): void {
    void this.lobby.start();
  }
}
