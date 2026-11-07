import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ApiError } from '../../../core/errors/api-error';
import { LobbyConnection } from '../../../core/lobby/lobby-connection';
import { TableService } from '../../../core/lobby/table-service';
import { ErrorMessages } from '../../../shared/error-messages/error-messages';

/**
 * Shown while the player holds a seat somewhere else. It asks rather than
 * decides: walking back to the lobby is not the same as giving up a chair,
 * and once there is a game to walk out of, leaving will cost something.
 */
@Component({
  selector: 'app-seated-notice',
  imports: [TranslatePipe, ErrorMessages],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './seated-notice.html',
  styleUrl: './seated-notice.scss',
})
export class SeatedNotice {
  private readonly lobby = inject(LobbyConnection);
  private readonly tables = inject(TableService);
  private readonly router = inject(Router);

  protected readonly table = this.lobby.myTable;
  protected readonly leaving = signal(false);
  protected readonly errors = signal<readonly ApiError[]>([]);

  protected returnToTable(): void {
    const table = this.table();

    if (table) {
      void this.router.navigate(['/table', table.tableId]);
    }
  }

  protected leave(): void {
    this.errors.set([]);
    this.leaving.set(true);

    this.tables.leave().subscribe({
      // Nothing to do on success: the seat is gone, so this notice goes too.
      next: () => this.leaving.set(false),
      error: (errors: ApiError[]) => {
        this.errors.set(errors);
        this.leaving.set(false);
      },
    });
  }
}
