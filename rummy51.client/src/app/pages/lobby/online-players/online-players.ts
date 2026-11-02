import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PlayerSummary } from '../../../core/auth/auth.models';

@Component({
  selector: 'app-online-players',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './online-players.html',
  styleUrl: './online-players.scss',
})
export class OnlinePlayers {
  readonly players = input.required<readonly PlayerSummary[]>();
  readonly currentPlayerId = input<string | null>(null);
}
