import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { failWithApiErrors } from '../errors/api-error';
import { CreateTableRequest, DrawnFrom, HandView, LobbyTable } from './lobby.models';

const API = '/api/tables';

/**
 * Opening, joining and leaving. Each answers to the caller alone; the new
 * state of the table reaches every other player over the live connection, so
 * nothing here has to tell anyone else.
 */
@Injectable({ providedIn: 'root' })
export class TableService {
  private readonly http = inject(HttpClient);

  open(request: CreateTableRequest): Observable<LobbyTable> {
    return this.http.post<LobbyTable>(API, request).pipe(failWithApiErrors());
  }

  join(tableId: string): Observable<LobbyTable> {
    return this.http.post<LobbyTable>(`${API}/${tableId}/join`, {}).pipe(failWithApiErrors());
  }

  /**
   * The cutter chose where to cut. The card there becomes the finishing card,
   * and the one after it is the first that will be dealt.
   */
  cut(tableId: string, position: number): Observable<HandView> {
    return this.http
      .post<HandView>(`${API}/${tableId}/cut`, { position })
      .pipe(failWithApiErrors());
  }

  /**
   * The first half of a turn: a card off the stock, or the one the player
   * before threw away.
   */
  draw(tableId: string, from: DrawnFrom): Observable<HandView> {
    return this.http.post<HandView>(`${API}/${tableId}/draw`, { from }).pipe(failWithApiErrors());
  }

  /**
   * The second half, which ends the turn and hands it on. The card is named
   * by the number it was given when it reached the hand: two sevens of hearts
   * are the same card, and a player throwing one of them means that one.
   */
  discard(tableId: string, cardId: number): Observable<HandView> {
    return this.http
      .post<HandView>(`${API}/${tableId}/discard`, { cardId })
      .pipe(failWithApiErrors());
  }

  /** Leaves whichever table the player is at, which is at most one. */
  leave(): Observable<void> {
    return this.http.post<void>(`${API}/leave`, {}).pipe(
      map(() => undefined),
      failWithApiErrors(),
    );
  }
}
