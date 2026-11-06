import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-icon-logout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-logout.html',
  styleUrl: './icon-logout.scss',
})
export class IconLogout {}
