import { ChangeDetectionStrategy, Component, ElementRef, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { IconLogout } from '../icons/icon-logout/icon-logout';
import { IconUser } from '../icons/icon-user/icon-user';
import { PlayerAvatar } from '../player-avatar/player-avatar';

/**
 * The signed-in player in the header: their picture, and behind it the few
 * things that belong to the account rather than to the page below.
 */
@Component({
  selector: 'app-profile-menu',
  imports: [RouterLink, TranslatePipe, IconLogout, IconUser, PlayerAvatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile-menu.html',
  styleUrl: './profile-menu.scss',
  host: {
    '(document:click)': 'closeUnlessInside($event)',
    '(document:keydown.escape)': 'close()',
  },
})
export class ProfileMenu {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly player = this.auth.currentPlayer;
  protected readonly open = signal(false);

  protected toggle(): void {
    this.open.update((open) => !open);
  }

  protected close(): void {
    this.open.set(false);
  }

  protected logout(): void {
    this.close();
    this.auth.logout().subscribe(() => this.router.navigate(['/']));
  }

  /** A click anywhere else dismisses the menu, including on the page behind it. */
  protected closeUnlessInside(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.close();
    }
  }
}
