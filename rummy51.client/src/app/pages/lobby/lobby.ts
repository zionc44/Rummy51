import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth-service';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { OnlinePlayers } from './online-players/online-players';
import { TableLists } from './table-lists/table-lists';

@Component({
  selector: 'app-lobby',
  imports: [PageShell, ProfileMenu, OnlinePlayers, TableLists],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './lobby.html',
  styleUrl: './lobby.scss',
})
export class Lobby {
  protected readonly auth = inject(AuthService);
  protected readonly lobby = inject(LobbyConnection);
}
