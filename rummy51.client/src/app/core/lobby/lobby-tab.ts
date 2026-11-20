import { Injectable, signal } from '@angular/core';
import { GameMode } from './lobby.models';

/**
 * Which list the lobby is showing. It lives out here and not on the page
 * because going to a table and coming back is not a change of mind: a
 * player who was looking at the survival tables is still looking at them,
 * and a page rebuilt from nothing would have forgotten.
 *
 * Only consulted on a phone; from the sm breakpoint every list is shown.
 */
@Injectable({ providedIn: 'root' })
export class LobbyTab {
  readonly open = signal<GameMode>('single');
}
