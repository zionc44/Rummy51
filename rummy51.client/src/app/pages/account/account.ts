import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { DisplayNameForm } from './display-name-form/display-name-form';
import { PasswordForm } from './password-form/password-form';

/**
 * What a player may change about themselves. The picture, the Google link and
 * the email address still cannot be changed here: the first two are waiting
 * their turn, and the third waits on a mail sender to confirm a new address.
 */
@Component({
  selector: 'app-account',
  imports: [RouterLink, TranslatePipe, PageShell, ProfileMenu, DisplayNameForm, PasswordForm],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './account.html',
  styleUrl: './account.scss',
})
export class Account {}
