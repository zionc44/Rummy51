import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { AvatarForm } from './avatar-form/avatar-form';
import { DisplayNameForm } from './display-name-form/display-name-form';
import { PasswordForm } from './password-form/password-form';

/**
 * What a player may change about themselves. The Google link and the email
 * address are still missing: the first is waiting its turn, and the second
 * waits on a mail sender to confirm a new address.
 */
@Component({
  selector: 'app-account',
  imports: [RouterLink, TranslatePipe, PageShell, ProfileMenu, AvatarForm, DisplayNameForm, PasswordForm],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './account.html',
  styleUrl: './account.scss',
})
export class Account {}
