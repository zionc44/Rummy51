import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ApiError } from '../../../core/errors/api-error';
import { LobbyConnection } from '../../../core/lobby/lobby-connection';
import { CreateTableRequest, GAME_MODES, GameMode, LobbyTable } from '../../../core/lobby/lobby.models';
import { TableService } from '../../../core/lobby/table-service';
import { ErrorMessages } from '../../../shared/error-messages/error-messages';
import { ModeIcon } from '../../../shared/mode-icon/mode-icon';
import { NewTable } from '../new-table/new-table';
import { TableList } from './table-list/table-list';

/**
 * All three lists are rendered always. From `sm` upwards there is room for all
 * three side by side and the tabs disappear; on a phone the tabs become an
 * accordion — the open one takes the width it needs, the others shrink to an
 * icon and their count. Which of the two applies is decided in the stylesheet,
 * so the template and the state stay the same at every width.
 */
@Component({
  selector: 'app-table-lists',
  imports: [TranslatePipe, ErrorMessages, ModeIcon, NewTable, TableList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-lists.html',
  styleUrl: './table-lists.scss',
})
export class TableLists {
  private readonly router = inject(Router);
  private readonly lobby = inject(LobbyConnection);
  private readonly tables = inject(TableService);

  protected readonly modes = GAME_MODES;

  /** Only consulted on a phone; wider screens show every list. */
  protected readonly openMode = signal<GameMode>('single');
  protected readonly creating = signal(false);
  protected readonly errors = signal<readonly ApiError[]>([]);

  /**
   * Rule 10.1: every table holding a chair for them. A row asks whether it
   * is one of these, and a table still waiting to deal is the one kind they
   * can hold only one of.
   */
  protected readonly seatedAt = computed(
    () => new Set(this.lobby.myTables().map((table) => table.tableId)),
  );

  /** Waiting somewhere is what stops them opening or joining another. */
  protected readonly waiting = computed(() =>
    this.lobby.myTables().some((table) => !table.started),
  );

  protected readonly byMode = computed(() => {
    const grouped: Record<GameMode, LobbyTable[]> = { single: [], survival: [], series: [] };

    for (const table of this.lobby.tables()) {
      grouped[table.mode].push(table);
    }

    return grouped;
  });

  protected readonly counts = computed<Record<GameMode, number>>(() => {
    const grouped = this.byMode();

    return {
      single: grouped.single.length,
      survival: grouped.survival.length,
      series: grouped.series.length,
    };
  });

  protected create(request: CreateTableRequest): void {
    this.creating.set(false);
    this.enter(this.tables.open(request));
  }

  protected join(tableId: string): void {
    this.enter(this.tables.join(tableId));
  }

  /** Rule 10.1: the chair is already theirs, so there is nothing to ask for. */
  protected returnTo(tableId: string): void {
    void this.router.navigate(['/table', tableId]);
  }

  /** Opening and joining differ only in the request; both end at the table. */
  private enter(request: Observable<LobbyTable>): void {
    this.errors.set([]);

    request.subscribe({
      next: (table) => void this.router.navigate(['/table', table.tableId]),
      error: (errors: ApiError[]) => this.errors.set(errors),
    });
  }
}
