import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { GameMode } from '../../core/lobby/lobby.models';
import { IconCards } from '../icons/icon-cards/icon-cards';
import { IconPulse } from '../icons/icon-pulse/icon-pulse';
import { IconStack } from '../icons/icon-stack/icon-stack';

/** The one place that decides which icon stands for which mode. */
@Component({
  selector: 'app-mode-icon',
  imports: [IconCards, IconPulse, IconStack],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mode-icon.html',
  styleUrl: './mode-icon.scss',
})
export class ModeIcon {
  readonly mode = input.required<GameMode>();
}
