import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { GameMode, LobbyTable, limitsOf } from '../../../../core/lobby/lobby.models';
import { ModeIcon } from '../../../../shared/mode-icon/mode-icon';
import { TableRow } from '../table-row/table-row';

/** One limit's tables, and the two numbers its heading carries. */
export interface LimitGroup {
  readonly limit: number;
  readonly tables: readonly LobbyTable[];
  readonly free: number;
}

/** The tables open in one mode, under a heading for each stake it is played for. */
@Component({
  selector: 'app-table-list',
  imports: [TranslatePipe, ModeIcon, TableRow],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-list.html',
  styleUrl: './table-list.scss',
})
export class TableList {
  readonly mode = input.required<GameMode>();
  readonly tables = input.required<readonly LobbyTable[]>();

  /** Passed along: a row needs both to know what it may offer. */
  readonly seatedAt = input<ReadonlySet<string>>(new Set());
  readonly waiting = input(false);

  readonly joined = output<string>();
  readonly returned = output<string>();

  /**
   * A table in play keeps its place in the list but sinks below the ones that
   * can still be joined, so the top of the list is always what is on offer.
   */
  protected readonly rows = computed(() =>
    [...this.tables()].sort((first, second) => Number(first.started) - Number(second.started)),
  );

  protected readonly limits = computed(() => limitsOf(this.mode()));

  /**
   * Every stake, whether or not anything is being played for it. A heading
   * that comes and goes is a heading nobody learns the place of, and an
   * empty one is an answer: there is nothing here, go and look at the next.
   */
  protected readonly groups = computed<readonly LimitGroup[]>(() =>
    this.limits().map((limit) => {
      const tables = this.rows().filter((table) => table.limit === limit);

      return { limit, tables, free: tables.filter((table) => !table.started).length };
    }),
  );

  /**
   * One open at a time, and the first with anything in it until somebody
   * says otherwise: a lobby that opens on an empty hundred reads as a lobby
   * with no tables at all. A choice already made is kept even when it is
   * the empty one, because it was made.
   */
  protected readonly open = linkedSignal<readonly LimitGroup[], number | null>({
    source: this.groups,
    computation: (groups, previous) => {
      const kept = previous?.value ?? null;

      if (kept !== null && groups.some((group) => group.limit === kept)) {
        return kept;
      }

      return (groups.find((group) => group.tables.length) ?? groups[0])?.limit ?? null;
    },
  });

  /** What a heading reads as, which is the same words the table page uses. */
  protected readonly stake = computed(() =>
    this.mode() === 'survival' ? 'lobby.points' : 'lobby.hands',
  );

  protected show(limit: number): void {
    this.open.set(limit);
  }
}
