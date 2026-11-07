import { ChangeDetectionStrategy, Component, OnDestroy, computed, effect, inject, input, signal } from '@angular/core';
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
 * How many rows the deck is laid out in for the cut. Eighteen cards to a row
 * at most, which is what leaves each of them enough width to be told apart
 * and aimed at.
 */
const FanRows = 6;

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
export class Table implements OnDestroy {
  readonly tableId = input('');

  private readonly auth = inject(AuthService);
  private readonly lobby = inject(LobbyConnection);
  private readonly tables = inject(TableService);
  private readonly router = inject(Router);

  protected readonly errors = signal<readonly ApiError[]>([]);

  protected readonly hand = this.lobby.hand;

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

  /** What the deck is made of, which matters before a card is dealt. */
  protected readonly deck = computed(() => {
    const table = this.table();

    if (!table) {
      return null;
    }

    return table.jokers === 0
      ? { key: 'lobby.noJokers', amount: {} }
      : { key: 'lobby.withJokers', amount: { '0': String(table.jokers) } };
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

  protected readonly canCut = computed(() => {
    const hand = this.hand();

    return hand?.phase === 'cutting' && this.ownSeat() === hand.cutterSeat;
  });

  protected readonly cutter = computed(() => {
    const hand = this.hand();

    return hand ? this.seats()[hand.cutterSeat]?.displayName ?? '' : '';
  });

  protected readonly perRow = computed(() => Math.ceil((this.hand()?.deckSize ?? 0) / FanRows));

  /**
   * The deck laid out for the cut. One row is unclickable on a phone - a
   * hundred cards across three hundred pixels is three pixels each - so it
   * is folded into the height there is.
   */
  protected readonly fan = computed(() => {
    const size = this.hand()?.deckSize ?? 0;
    const perRow = this.perRow();

    return Array.from({ length: FanRows }, (_, row) =>
      Array.from(
        { length: Math.max(0, Math.min(perRow, size - row * perRow)) },
        (_, column) => row * perRow + column,
      ),
    ).filter((row) => row.length > 0);
  });

  constructor() {
    effect(() => {
      const table = this.tableId();

      // Only once the hub is up: watching is a call to it, not a wish.
      if (this.lobby.connected() && table) {
        void this.lobby.watch(table);
      }
    });
  }

  ngOnDestroy(): void {
    void this.lobby.unwatch(this.tableId());
  }

  protected cut(position: number): void {
    this.errors.set([]);

    this.tables.cut(this.tableId(), position).subscribe({
      // Nothing to do: the hub tells everyone at the table, this page included.
      next: () => undefined,
      error: (errors: ApiError[]) => this.errors.set(errors),
    });
  }

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
