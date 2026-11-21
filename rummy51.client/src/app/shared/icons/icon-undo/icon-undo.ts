import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-icon-undo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './icon-undo.html',
  styleUrl: './icon-undo.scss',
})
export class IconUndo {}
