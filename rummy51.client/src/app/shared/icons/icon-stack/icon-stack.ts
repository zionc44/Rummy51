import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-icon-stack',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-stack.html',
  styleUrl: './icon-stack.scss',
})
export class IconStack {}
