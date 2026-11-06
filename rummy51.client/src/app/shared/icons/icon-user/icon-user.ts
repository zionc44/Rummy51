import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-icon-user',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-user.html',
  styleUrl: './icon-user.scss',
})
export class IconUser {}
