import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-icon-globe',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-globe.html',
  styleUrl: './icon-globe.scss',
})
export class IconGlobe {}
