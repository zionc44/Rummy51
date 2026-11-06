import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-icon-pulse',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-pulse.html',
  styleUrl: './icon-pulse.scss',
})
export class IconPulse {}
