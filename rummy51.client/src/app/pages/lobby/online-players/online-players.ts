import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PlayerSummary } from '../../../core/auth/auth.models';

@Component({
  selector: 'app-online-players',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="w-full max-w-sm">
      <h2 class="mb-3 text-sm font-bold uppercase tracking-wide text-white/50">
        {{ 'lobby.onlinePlayers' | translate }} ({{ players().length }})
      </h2>

      @if (players().length) {
        <ul class="flex flex-col gap-1.5">
          @for (player of players(); track player.playerId) {
            <li class="flex items-center gap-2.5 rounded-lg bg-white/5 px-4 py-2.5">
              <span class="size-2 rounded-full bg-felt-500" aria-hidden="true"></span>
              <span class="font-medium">{{ player.displayName }}</span>

              @if (player.playerId === currentPlayerId()) {
                <span class="text-sm text-white/40">{{ 'lobby.you' | translate }}</span>
              }
            </li>
          }
        </ul>
      } @else {
        <p class="rounded-lg bg-white/5 px-4 py-2.5 text-white/40">
          {{ 'lobby.nobodyHere' | translate }}
        </p>
      }
    </section>
  `,
})
export class OnlinePlayers {
  readonly players = input.required<readonly PlayerSummary[]>();
  readonly currentPlayerId = input<string | null>(null);
}
