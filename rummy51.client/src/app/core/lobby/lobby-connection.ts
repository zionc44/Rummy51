import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { AuthService } from '../auth/auth-service';
import { HandView, LobbyPlayer, LobbyTable } from './lobby.models';

const HUB_URL = '/hubs/lobby';

/** Names the server sends on. Must match LobbyEvents on the hub. */
const enum LobbyEvent {
  OnlinePlayersChanged = 'OnlinePlayersChanged',
  TablesChanged = 'TablesChanged',
  HandChanged = 'HandChanged',
  SignedInElsewhere = 'SignedInElsewhere',
}

/**
 * The live connection, held for as long as the session is. It is not the
 * lobby page's to own: walking to a table would close it, and the server
 * reads a closed connection as a player who has left their seat.
 */
@Injectable({ providedIn: 'root' })
export class LobbyConnection {
  private readonly auth = inject(AuthService);

  private readonly players = signal<readonly LobbyPlayer[]>([]);
  private readonly open = signal<readonly LobbyTable[]>([]);
  private readonly replaced = signal(false);
  private readonly live = signal(false);
  private readonly playing = signal<HandView | null>(null);

  readonly onlinePlayers = this.players.asReadonly();
  readonly tables = this.open.asReadonly();

  /** The hand at the table being watched, if one is. */
  readonly hand = this.playing.asReadonly();

  /**
   * True once the hub has answered. Until then an empty list of tables means
   * nothing has arrived, not that there are none - and a table page cannot
   * tell a table that closed from one it has not heard about yet.
   */
  readonly connected = this.live.asReadonly();

  /**
   * The table the player is sitting at, which is at most one. It survives
   * walking away to the lobby: only leaving on purpose, or going offline,
   * gives up a seat.
   */
  readonly myTable = computed(() => {
    const me = this.auth.currentPlayer()?.playerId ?? null;

    return this.open().find((table) => table.seats.some((seat) => seat.playerId === me)) ?? null;
  });

  /** True once a newer connection elsewhere has taken this player's place. */
  readonly signedInElsewhere = this.replaced.asReadonly();

  private connection: HubConnection | null = null;

  constructor() {
    effect(() => {
      if (this.auth.isSignedIn()) {
        void this.start();
      } else {
        void this.stop();
      }
    });
  }

  async start(): Promise<void> {
    if (this.connection) {
      return;
    }

    this.replaced.set(false);
    this.connection = this.build();

    await this.connection.start();
    this.live.set(true);
  }

  async stop(): Promise<void> {
    const connection = this.connection;
    this.connection = null;

    this.live.set(false);
    this.players.set([]);
    this.open.set([]);
    this.playing.set(null);
    await connection?.stop();
  }

  /**
   * Asks the hub for this one table's hand and to be told when it changes.
   * A group rather than a broadcast, because the cards it will carry are
   * about to be somebody's secret.
   */
  async watch(tableId: string): Promise<void> {
    const hand = await this.connection?.invoke<HandView | null>('Watch', tableId);

    this.playing.set(hand ?? null);
  }

  async unwatch(tableId: string): Promise<void> {
    this.playing.set(null);
    await this.connection?.invoke('Unwatch', tableId);
  }

  private build(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      // A dropped connection is the normal case on a phone that locks or a
      // network that blinks; reconnecting silently is what a player expects.
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on(LobbyEvent.OnlinePlayersChanged, (players: LobbyPlayer[]) =>
      this.players.set(players),
    );

    connection.on(LobbyEvent.TablesChanged, (tables: LobbyTable[]) => this.open.set(tables));

    connection.on(LobbyEvent.HandChanged, (hand: HandView | null) => this.playing.set(hand));

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
