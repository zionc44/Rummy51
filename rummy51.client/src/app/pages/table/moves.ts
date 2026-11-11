import { Signal, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { Fit, Swap } from '../../core/cards/card';
import { ApiError } from '../../core/errors/api-error';
import { DrawnFrom, HandView } from '../../core/lobby/lobby.models';
import { TableService } from '../../core/lobby/table-service';
import { CardJourney } from './card-journey';

/**
 * What this player does at the table, and the only thing that comes back
 * from having done it.
 *
 * Which move a gesture means is not decided here. One pile is two moves
 * depending on whether a card has been taken yet, and a card let go of
 * means something only where it was let go - both of those are questions
 * about this screen, and the page answers them. This is what is sent once
 * they have been answered, which is also where going down and adding to a
 * meld will arrive.
 */
export class Moves {
  constructor(
    private readonly tableId: Signal<string>,
    private readonly tables: TableService,
    private readonly router: Router,
    private readonly journey: CardJourney,
  ) {}

  /** What the server refused, in codes the client words itself. */
  readonly errors = signal<readonly ApiError[]>([]);

  cut(position: number): void {
    this.send(this.tables.cut(this.tableId(), position));
  }

  draw(from: DrawnFrom): void {
    this.journey.leaving(from);
    this.send(this.tables.draw(this.tableId(), from));
  }

  discard(cardId: number): void {
    this.journey.thrown(cardId);
    this.send(this.tables.discard(this.tableId(), cardId));
  }

  lay(cardIds: readonly number[]): void {
    this.send(this.tables.lay(this.tableId(), cardIds));
  }

  /** A card out of the hand and onto a meld already lying on the table. */
  addToMeld(cardId: number, fit: Fit): void {
    this.send(this.tables.addToMeld(this.tableId(), cardId, fit));
  }

  /** A joker bought out of a meld with the card it was standing in for. */
  swapForJoker(cardId: number, swap: Swap): void {
    this.send(this.tables.swapForJoker(this.tableId(), cardId, swap));
  }

  undo(): void {
    this.send(this.tables.undo(this.tableId()));
  }

  /**
   * Giving up the seat, which is the one move whose answer matters here:
   * nobody is left at the table to be told about it.
   */
  leave(): void {
    this.errors.set([]);

    this.tables.leave().subscribe({
      next: () => void this.router.navigate(['/lobby']),
      error: (errors: ApiError[]) => this.errors.set(errors),
    });
  }

  backToLobby(): void {
    void this.router.navigate(['/lobby']);
  }

  /**
   * A move, and nothing to do with the answer. The hub tells everyone at the
   * table what came of it, this page included, so the only thing the reply
   * is good for here is the complaint.
   */
  private send(move: Observable<HandView>): void {
    this.errors.set([]);

    move.subscribe({
      next: () => undefined,
      error: (errors: ApiError[]) => this.errors.set(errors),
    });
  }
}
