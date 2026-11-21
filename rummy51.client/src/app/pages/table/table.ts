import { ChangeDetectionStrategy, Component, ElementRef, Injector, OnDestroy, afterNextRender, afterRenderEffect, computed, effect, inject, input, signal, untracked, viewChild, viewChildren } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { HandResult } from './hand-result/hand-result';
import { TableStanding } from './table-standing/table-standing';
import { ArrangedHand } from '../../core/cards/arranged-hand';
import { Fit, Swap } from '../../core/cards/card';
import { Least } from '../../core/cards/melds';
import { HandPhase, HandView } from '../../core/lobby/lobby.models';
import { TableService } from '../../core/lobby/table-service';
import { Asking } from './asking';
import { Turn } from './turn';
import { Notice } from '../../shared/notice/notice';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { whatCrossed } from './crossings';
import { Journeys } from './journeys';
import { Moves } from './moves';
import { Reading } from './reading';
import { Reckoning } from './reckoning';
import { Seating } from './seating';
import { CardMove, CardRelease } from './player-hand/hand-drag';
import { PlayerHand } from './player-hand/player-hand';
import { TableCut } from './table-cut/table-cut';
import { TableSeat } from './table-seat/table-seat';
import { TableMelds } from './table-melds/table-melds';
import { TableTally } from './table-tally/table-tally';
import { TablePiles } from './table-piles/table-piles';
import { TableTerms } from './table-terms/table-terms';
import { TableTools } from './table-tools/table-tools';
import { KeepThree } from './keep-three/keep-three';
import { ForceRedeal } from './force-redeal/force-redeal';
import { StillThere } from './still-there/still-there';
import { RedealVote } from './redeal-vote/redeal-vote';

/** The refusal that says a card taken off the table is still in the hand. */
const CardOwed = 'TakenCardStillHeld';

/**
 * The table, before and after it fills. Nothing changes screens when the last
 * seat is taken: the felt between the seats is already everything left over on
 * the screen, and that is where the melds will go. The player's own place is
 * the near one, where their hand will sit beneath their name.
 *
 * What is left here is the wiring. Where things are on the screen, and the
 * gesture that means one move at one moment and another at another - those
 * are questions only a page can answer. Everything that is worked out
 * rather than measured is worked out beside it.
 */
@Component({
  selector: 'app-table',
  imports: [
    TranslatePipe,
    HandResult,
    Notice,
    PageShell,
    PlayerHand,
    ProfileMenu,
    TableCut,
    TableMelds,
    TableStanding,
    TableTally,
    TableSeat,
    TableTerms,
    TablePiles,
    TableTools,
    RedealVote,
    ForceRedeal,
    KeepThree,
    StillThere,
  ],
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
  private readonly injector = inject(Injector);
  private readonly element: ElementRef<HTMLElement> = inject(ElementRef);

  protected readonly hand = this.lobby.hand;

  protected readonly myCards = this.lobby.myCards;

  /** The hand as its owner has arranged it, which is theirs and not the server's. */
  protected readonly arranged = new ArrangedHand(this.myCards);

  private readonly theCut = viewChild<TableCut>('theCut');
  private readonly piles = viewChild<TablePiles>(TablePiles);
  private readonly nearHand = viewChild<PlayerHand>('nearHand');
  private readonly otherSpots = viewChildren<TableSeat>('seatSpot');
  private seenPhase: HandPhase | null = null;
  private readonly ticking = signal(Date.now());
  private readonly beat: number;
  private seen: HandView | null = null;

  /**
   * Everything on this table that is seen to travel, and the places it
   * travels between. This page says where things are and that does the
   * rest, so what is left here is what the table is rather than what it
   * is doing.
   *
   * It is built before the seating it asks questions of, because the deal
   * is one of the things the seating reads. Every question here is asked
   * through a closure for that reason, and the type is written out because
   * the two of them refer to each other and nothing could work it out.
   */
  protected readonly journeys: Journeys = new Journeys({
    fan: () => this.theCut()?.cards() ?? [],
    stock: () => this.piles()?.stockSpot(),
    discards: () => this.piles()?.discardSpot(),
    finishing: () => this.piles()?.finishingSpot(),
    inHand: (step) => this.nearHand()?.landingSpot(step),
    cardAt: (id) => this.nearHand()?.cardAt(id),
    across: (seat) => this.otherSeatAt(seat),
    mine: (seat) => this.seating.mine(seat),
    nearSeat: () => this.seating.nearSeat(),
    ownSeat: () => this.seating.ownSeat(),
    page: () => this.element.nativeElement,
  });

  /** What this player does at the table, and what the server said about it. */
  protected readonly moves = new Moves(
    this.tableId,
    this.tables,
    this.router,
    this.journeys.card,
  );

  /** Null while the hub has not said, and once the table has closed. */
  protected readonly table = computed(
    () => this.lobby.tables().find((table) => table.tableId === this.tableId()) ?? null,
  );

  /** Told apart from "not heard yet" only once the connection has answered. */
  protected readonly gone = computed(() => this.lobby.connected() && this.table() === null);

  /**
   * Who is around this table and what each of their places shows. The page
   * says where everything is on the screen; this says what belongs there.
   */
  protected readonly seating: Seating = new Seating(
    this.table,
    this.hand,
    computed(() => this.auth.currentPlayer()?.playerId ?? null),
    this.journeys.deal,
    this.arranged.cards,
  );

  /** A hand in front of this player to sort, to drag and to be told about. */
  protected readonly arrangeable = computed(
    () => !this.seating.filling() && this.seating.seated() && this.myCards().length > 0,
  );

  protected readonly canCut = computed(() => {
    const hand = this.hand();

    return hand?.phase === 'cutting' && this.seating.ownSeat() === hand.cutterSeat;
  });

  /** What the table is asking this player, which is chapter 12. */
  protected readonly asking = new Asking(
    this.hand,
    this.seating.seats,
    this.seating.ownSeat,
    this.lobby.aboutARedeal,
    this.ticking,
    this.moves,
  );

  /** What this player may do in the hand as it stands, for the screen to read. */
  protected readonly turn = new Turn(
    this.hand,
    this.seating.ownSeat,
    this.journeys.deal.animating,
    this.arranged.picked,
  );

  /** What the hand in front of this player comes to, as they have laid it. */
  protected readonly reading = new Reading(
    this.hand,
    this.myCards,
    this.arranged.picked,
    this.seating.nearSeat,
    () => this.nearHand()?.inMelds() ?? [],
  );

  /** How the hand came out, and where the game stands after it. */
  protected readonly reckoning = new Reckoning(
    this.hand,
    this.table,
    this.seating.seats,
    this.seating.ownSeat,
    this.moves,
  );

  constructor() {
    // Every quarter second, which is finer than the number it draws so that
    // the number never appears to stick.
    this.beat = window.setInterval(() => this.ticking.set(Date.now()), 250);

    effect(() => {
      const table = this.tableId();

      // Only once the hub is up: watching is a call to it, not a wish.
      if (this.lobby.connected() && table) {
        void this.lobby.watch(table);
      }
    });

    effect(() => {
      const phase = this.hand()?.phase ?? null;
      const before = this.seenPhase;
      this.seenPhase = phase;

      // The change is the event, not the state. Somebody opening the page
      // after the cut should not be shown it happening again.
      if (phase === 'cutting') {
        this.journeys.deal.forget();
        this.arranged.forget();
      }

      this.asking.settle(phase);
      this.reckoning.settle(phase);

      const hand = this.hand();

      if (before === 'cutting' && phase === 'dealing' && hand !== null) {
        this.journeys.deal.begin(hand);
      }
    });

    // Whatever crossed between a hand and the felt is flown after the
    // render that moved it. Reading where everything is and then asking
    // what moved are the two halves of one measurement, so they are two
    // phases of one effect and cannot be separated by a render.
    //
    // Nobody tells this that a move is coming. Every player at the table
    // learns of one the same way - the table changed - so every player
    // sees the same cards travel, and the one who made the move is not a
    // special case.
    afterRenderEffect({
      earlyRead: () => {
        const hand = this.hand();
        const before = this.seen;

        // The hand as the table sends it and the hand as its owner has laid
        // it out, because both of them move cards: a run laid down leaves
        // from wherever the player last sorted it to, and a measurement
        // taken before that sort would send it from somewhere it has not
        // been for a while.
        this.arranged.cards();

        this.seen = hand;
        this.journeys.crossing.remember();

        return whatCrossed(before, hand);
      },
      write: (crossed) => {
        const what = crossed();

        untracked(() => this.journeys.fly(what));
      },
    });

    // A card drawn is flown in once it is lying where it will lie, which is
    // one render after the hand has heard of it. By then it is also the card
    // standing out of the hand, which is how it is found: a card just drawn
    // is picked the moment it lands. Nothing happens here unless a draw was
    // asked for, so a sort or a throw passes straight through.
    effect(() => {
      this.arranged.cards();

      const landed = untracked(() => this.arranged.picked());

      afterNextRender(() => this.journeys.card.arrived(landed), { injector: this.injector });
    });

    // A throw refused because a card taken off the table is still in the
    // hand. The player had picked out the card they meant to throw, which
    // is now the wrong one to be looking at: the one that has to go back
    // takes its place, so it is standing clear when the notice is read and
    // the next place pressed lays it.
    effect(() => {
      const owing = this.moves.errors().some((error) => error.code === CardOwed);

      if (!owing) {
        return;
      }

      untracked(() => {
        const owed = this.myCards().find((card) => card.owed);

        if (owed !== undefined) {
          this.arranged.singleOut(owed.id);
        }
      });
    });
  }

  ngOnDestroy(): void {
    window.clearInterval(this.beat);
    this.journeys.land();
    void this.lobby.unwatch(this.tableId());
  }

  protected moveCard(move: CardMove): void {
    this.arranged.move(move.ids, move.to, move.firstLine);
  }

  /**
   * One pile and two moves, never both at once: until a card has been taken
   * the pile is something to take from, and afterwards something to throw
   * onto.
   */
  protected pileClicked(): void {
    if (this.turn.canTakeDiscard()) {
      this.moves.draw('discards');

      return;
    }

    const picked = this.arranged.picked();

    if (this.turn.canThrow() && picked !== null) {
      this.moves.discard(picked);
    }
  }

  /**
   * A run tapped by its band, which lays it on the table. The window is the
   * same one a card is thrown in - after taking one and before giving one
   * back - because laying cards down ends neither half of a turn.
   *
   * Rule 6.2: a meld that is the whole of a hand lays all but its last
   * card, because a hand emptied onto the table has nothing left to end
   * the turn with. The last is the card that is always safe to hold back -
   * a run shortened at its end is still a run, and four of a rank are a
   * set at three - and it is the one that then goes out as the throw.
   */
  protected layDown(ids: readonly number[]): void {
    if (!this.turn.toThrow()) {
      return;
    }

    const whole = ids.length === this.myCards().length;

    this.moves.lay(whole && ids.length > Least ? ids.slice(0, -1) : ids);
  }

  /** One of the places offered pressed, which sends the picked card to it. */
  protected addToMeld(fit: Fit): void {
    const picked = this.arranged.picked();

    if (picked !== null) {
      this.moves.addToMeld(picked, fit);
    }
  }

  /** A lifted joker pressed, which buys it with the card that was picked. */
  protected swapForJoker(swap: Swap): void {
    const picked = this.arranged.picked();

    if (picked !== null) {
      this.moves.swapForJoker(picked, swap);
    }
  }

  /** Rule 10.6: the same saying-so the page makes when it opens. */
  protected stillHere(): void {
    void this.lobby.watch(this.tableId());
  }

  /**
   * A card let go of outside the hand. One place out there means something,
   * and one card at a time can be thrown onto it.
   */
  protected cardReleased(release: CardRelease): void {
    const pile = this.piles()?.discardSpot()?.getBoundingClientRect();

    if (!this.turn.toThrow() || release.ids.length !== 1 || pile === undefined) {
      return;
    }

    const { x, y } = release.at;

    if (x >= pile.left && x <= pile.right && y >= pile.top && y <= pile.bottom) {
      this.moves.discard(release.ids[0]);
    }
  }

  /** The place across the table at that seat, if it is one of them. */
  private otherSeatAt(seat: number): TableSeat | undefined {
    const among = this.seating.others().findIndex((place) => place.at === seat);

    return among === -1 ? undefined : this.otherSpots()[among];
  }
}
