import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LobbyTable } from '../../../core/lobby/lobby.models';

/**
 * The terms a table was opened on, in one line above the felt: the mode it
 * is played in, how long the game is, and what the deck is made of. None of
 * them changes once the table exists, and together they are what a player
 * arriving at it needs to know before a card is dealt.
 *
 * The server sends codes and never words, so the wording is built here out
 * of translation keys - which is the whole reason this is not three
 * interpolations in the page that holds it.
 */
@Component({
  selector: 'app-table-terms',
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './table-terms.html',
  styleUrl: './table-terms.scss',
})
export class TableTerms {
  readonly table = input.required<LobbyTable>();

  /** Points that end a survival game, or hands in a series. Nothing for one. */
  protected readonly limit = computed(() => {
    const table = this.table();

    return table.limit === null
      ? null
      : {
          key: table.mode === 'survival' ? 'lobby.points' : 'lobby.hands',
          amount: { '0': String(table.limit) },
        };
  });

  /** What the deck is made of, which matters before a card is dealt. */
  protected readonly deck = computed(() => {
    const jokers = this.table().jokers;

    return jokers === 0
      ? { key: 'lobby.noJokers', amount: {} }
      : { key: 'lobby.withJokers', amount: { '0': String(jokers) } };
  });
}
