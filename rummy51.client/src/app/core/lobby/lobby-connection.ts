import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { PlayerSummary } from '../auth/auth.models';

const HUB_URL = '/hubs/lobby';

/** Names the server sends on. Must match LobbyEvents on the hub. */
const enum LobbyEvent {
  OnlinePlayersChanged = 'OnlinePlayersChanged',
  SignedInElsewhere = 'SignedInElsewhere',
}

/**
 * Owns the lobby's live connection. The authentication cookie rides along with
 * the handshake, so the hub knows who is calling without being told.
 */
@Injectable({ providedIn: 'root' })
export class LobbyConnection {
  private readonly players = signal<readonly PlayerSummary[]>([]);
  private readonly replaced = signal(false);

  readonly onlinePlayers = this.players.asReadonly();

  /** True once a newer connection elsewhere has taken this player's place. */
  readonly signedInElsewhere = this.replaced.asReadonly();

  private connection: HubConnection | null = null;

  async start(): Promise<void> {
    if (this.connection) {
      return;
    }

    this.replaced.set(false);
    this.connection = this.build();

    await this.connection.start();
  }

  async stop(): Promise<void> {
    const connection = this.connection;
    this.connection = null;

    this.players.set([]);
    await connection?.stop();
  }

  private build(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      // A dropped connection is the normal case on a phone that locks or a
      // network that blinks; reconnecting silently is what a player expects.
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on(LobbyEvent.OnlinePlayersChanged, (players: PlayerSummary[]) =>
      this.players.set(players),
    );

    connection.on(LobbyEvent.SignedInElsewhere, () => {
      this.replaced.set(true);
      // The hub has already forgotten this connection, so closing it here is
      // tidiness rather than enforcement. It also leaves start() free to run
      // again if the player decides to take their place back.
      void this.stop();
    });

    return connection;
  }
}
