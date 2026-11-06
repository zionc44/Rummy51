import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-icon-cards',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-cards.html',
  styleUrl: './icon-cards.scss',
})
export class IconCards {}
