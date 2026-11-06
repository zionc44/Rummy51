import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-icon-rotate',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-rotate.html',
  styleUrl: './icon-rotate.scss',
})
export class IconRotate {}
