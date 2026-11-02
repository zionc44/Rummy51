import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { PageShell } from '../../shared/page-shell/page-shell';
import { OnlinePlayers } from './online-players/online-players';

@Component({
  selector: 'app-lobby',
  imports: [TranslatePipe, PageShell, OnlinePlayers],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './lobby.html',
})
export class Lobby implements OnInit, OnDestroy {
  protected readonly auth = inject(AuthService);
  protected readonly lobby = inject(LobbyConnection);

  private readonly router = inject(Router);

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

  protected logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/']));
  }
}
