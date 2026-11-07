import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { ApiError } from '../../core/errors/api-error';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { TableService } from '../../core/lobby/table-service';
import { ErrorMessages } from '../../shared/error-messages/error-messages';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { TableSeat } from './table-seat/table-seat';

/**
 * The table, before and after it fills. Nothing changes screens when the last
 * seat is taken: the felt between the seats is already everything left over on
 * the screen, and that is where the melds will go. The player's own place is
 * the near one, where their hand will sit beneath their name.
 */
@Component({
  selector: 'app-table',
  imports: [TranslatePipe, ErrorMessages, PageShell, ProfileMenu, TableSeat],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table.html',
  styleUrl: './table.scss',
})
export class Table {
  readonly tableId = input('');

  private readonly auth = inject(AuthService);
  private readonly lobby = inject(LobbyConnection);
  private readonly tables = inject(TableService);
  private readonly router = inject(Router);

  protected readonly errors = signal<readonly ApiError[]>([]);

  /** Null while the hub has not said, and once the table has closed. */
  protected readonly table = computed(
    () => this.lobby.tables().find((table) => table.tableId === this.tableId()) ?? null,
  );

  /** Told apart from "not heard yet" only once the connection has answered. */
  protected readonly gone = computed(() => this.lobby.connected() && this.table() === null);

  protected readonly seats = computed(() => this.table()?.seats ?? []);

  protected readonly taken = computed(() => this.seats().filter((seat) => seat.playerId).length);

  protected readonly detail = computed(() => {
    const table = this.table();

    return table?.limit == null
      ? null
      : {
          key: table.mode === 'survival' ? 'lobby.points' : 'lobby.hands',
          amount: { '0': String(table.limit) },
        };
  });

  /** -1 while the viewer is not sitting at this table. */
  private readonly ownSeat = computed(() => {
    const me = this.auth.currentPlayer()?.playerId ?? null;

    return this.seats().findIndex((seat) => seat.playerId === me);
  });

  protected readonly seated = computed(() => this.ownSeat() !== -1);

  protected readonly near = computed(() => this.seats()[Math.max(0, this.ownSeat())] ?? null);

  /**
   * Everyone else, counting round the table from the player's own seat, so
   * the row across from them reads in the order play will take.
   */
  protected readonly others = computed(() => {
    const seats = this.seats();
    const from = Math.max(0, this.ownSeat());

    return Array.from(
      { length: Math.max(0, seats.length - 1) },
      (_, step) => seats[(from + step + 1) % seats.length],
    );
  });

  protected leave(): void {
    this.errors.set([]);

    this.tables.leave().subscribe({
      next: () => void this.router.navigate(['/lobby']),
      error: (errors: ApiError[]) => this.errors.set(errors),
    });
  }

  protected backToLobby(): void {
    void this.router.navigate(['/lobby']);
  }
}
