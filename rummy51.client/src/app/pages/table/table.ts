import { ChangeDetectionStrategy, Component, ElementRef, Injector, OnDestroy, afterNextRender, afterRenderEffect, computed, effect, inject, input, signal, untracked, viewChild, viewChildren } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth-service';
import { LobbyConnection } from '../../core/lobby/lobby-connection';
import { HandResult, ResultRow } from './hand-result/hand-result';
import { TableStanding } from './table-standing/table-standing';
import { ArrangedHand } from '../../core/cards/arranged-hand';
import { Fit, Swap } from '../../core/cards/card';
import { GoingDown, Least, hasCleanRun, showingWorth } from '../../core/cards/melds';
import { NeverWentDown, worthOfHand } from '../../core/cards/worth';
import { HandPhase, HandView } from '../../core/lobby/lobby.models';
import { TableService } from '../../core/lobby/table-service';
import { Asking } from './asking';
import { Turn } from './turn';
import { Notice } from '../../shared/notice/notice';
import { PageShell } from '../../shared/page-shell/page-shell';
import { ProfileMenu } from '../../shared/profile-menu/profile-menu';
import { CardJourney } from './card-journey';
import { Crossings, whatCrossed } from './crossings';
import { MeldJourney } from './meld-journey';
import { Moves } from './moves';
import { Seating } from './seating';
import { Dealing, Spots } from './dealing';
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

  protected sortHand(): void {
    this.arranged.sort();
  }

  protected moveCard(move: CardMove): void {
    this.arranged.move(move.ids, move.to, move.firstLine);
  }

  private readonly theCut = viewChild<TableCut>('theCut');
  private readonly piles = viewChild<TablePiles>(TablePiles);
  private readonly nearHand = viewChild<PlayerHand>('nearHand');
  private readonly otherSpots = viewChildren<TableSeat>('seatSpot');
  private seenPhase: HandPhase | null = null;
  private readonly ticking = signal(Date.now());
  private readonly beat: number;
  private seen: HandView | null = null;

  /** Everything the cut and the deal have to aim at, found in this view. */
  private spots(): Spots {
    return {
      fanCards: () => this.theCut()?.cards() ?? [],
      pile: () => this.piles()?.stockSpot(),
      finishingSlot: () => this.piles()?.finishingSpot(),
      faceUp: (seat, step) => this.faceUpSpot(seat, step),
      dealt: (seat, step) =>
        this.seating.mine(seat)
          ? this.nearHand()?.landingSpot(step)
          : this.otherSeatAt(seat)?.pileSpot(),
    };
  }

  /**
   * Where a card going face up at a seat comes to rest. The near place keeps
   * its cards in a hand of its own; every other seat keeps the few it shows
   * beside the picture.
   */
  private faceUpSpot(seat: number, step: number): HTMLElement | undefined {
    return seat === this.seating.nearSeat()
      ? this.nearHand()?.landingSpot(step)
      : this.otherSeatAt(seat)?.landingSpot(step);
  }

  /**
   * Where the cards of a hand nobody may see are drawn, which is a pile of
   * backs beside that player. The near seat shows no pile - its cards are
   * all there to be looked at - and needs none: they are found where they
   * lie.
   */
  private pileAt(seat: number): HTMLElement | undefined {
    return this.otherSeatAt(seat)?.backSpot();
  }

  /** The place across the table at that seat, if it is one of them. */
  private otherSeatAt(seat: number): TableSeat | undefined {
    const among = this.seating.others().findIndex((place) => place.at === seat);

    return among === -1 ? undefined : this.otherSpots()[among];
  }

  /**
   * The cut and the deal played out on screen. This page says where things
   * are and that does the rest, so what is left here is what the table is
   * rather than what it is doing.
   */
  private readonly show = new Dealing(this.spots());

  /** One card's journey between a pile and the hand, drawn or thrown. */
  private readonly journey = new CardJourney({
    stock: () => this.piles()?.stockSpot(),
    discards: () => this.piles()?.discardSpot(),
    finishing: () => this.piles()?.finishingSpot(),
    cardAt: (id) => this.nearHand()?.cardAt(id),
  });

  /**
   * Cards seen to cross between the hand and the felt, whichever way they
   * are going. It is given the whole page to look in, because that is the
   * only thing that holds both ends of the journey.
   */
  private readonly crossing = new MeldJourney(() => this.element.nativeElement);

  /** What this player does at the table, and what the server said about it. */
  protected readonly moves = new Moves(this.tableId, this.tables, this.router, this.journey);

  /** True from the cut until the deck has settled, which keeps both on screen. */
  protected readonly collapsing = this.show.collapsing;

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
  protected readonly seating = new Seating(
    this.table,
    this.hand,
    computed(() => this.auth.currentPlayer()?.playerId ?? null),
    this.show,
    this.arranged.cards,
  );

  /** How far the cut has got in turning its cards over. */
  protected readonly turned = this.show.turned;

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
  );

  /** What this player may do in the hand as it stands, for the screen to read. */
  protected readonly turn = new Turn(
    this.hand,
    this.seating.ownSeat,
    this.show.animating,
    this.arranged.picked,
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
        this.show.forget();
        this.arranged.forget();
      }

      this.asking.settle(phase);

      const hand = this.hand();

      if (before === 'cutting' && phase === 'dealing' && hand !== null) {
        this.show.begin(hand);
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
        this.crossing.remember();

        return whatCrossed(before, hand);
      },
      write: (crossed) => {
        const what = crossed();

        untracked(() => this.fly(what));
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

      afterNextRender(() => this.journey.arrived(landed), { injector: this.injector });
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

  /**
   * What crossed the table, flown. A throw of this player's own was already
   * sent from the card itself as they let go of it; everybody else's comes
   * out of a pile of backs, because that is all anyone was ever shown of it.
   */
  private fly(what: Crossings): void {
    if (what.meld !== null) {
      this.crossing.crossed(what.meld.ids, this.pileAt(what.meld.seat));
    }

    if (what.thrownFrom !== null && what.thrownFrom !== this.seating.ownSeat()) {
      this.journey.threwFrom(this.pileAt(what.thrownFrom));
    }
  }

  ngOnDestroy(): void {
    window.clearInterval(this.beat);
    this.show.stop();
    this.journey.land();
    this.crossing.land();
    void this.lobby.unwatch(this.tableId());
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

  /**
   * Where the card the player has picked out could join what is lying on
   * the table. The places are the server's answer, carried on the card
   * itself, so the screen offers them without knowing the rule.
   */
  protected readonly fits = computed(() => this.thePicked()?.fits ?? []);

  /** And the jokers it is standing in for, which it could buy out. */
  protected readonly buys = computed(() => this.thePicked()?.buys ?? []);

  private readonly thePicked = computed(() => {
    const picked = this.arranged.picked();

    return this.myCards().find((card) => card.id === picked);
  });

  /** One of those places pressed, which sends the card to it. */
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

  /** The runs and sets this hand is lying in, as its owner has laid them. */
  private readonly inMelds = computed(() => this.nearHand()?.inMelds() ?? []);

  /** What the melds come to, counted the way the fifty-one counts. */
  protected readonly meldPoints = computed(() => showingWorth(this.inMelds()));

  protected readonly hasCleanRun = computed(() => this.inMelds().some(hasCleanRun));

  /** Whether this player has shown a hand, which the whole table may see. */
  protected readonly wentDown = computed(
    () => this.hand()?.wentDown[this.seating.nearSeat()] ?? false,
  );

  /**
   * What this hand costs if the hand ends now. A flat hundred until its
   * owner has gone down, whatever they are holding.
   */
  protected readonly worth = computed(() =>
    this.wentDown() ? worthOfHand(this.myCards()) : NeverWentDown,
  );

  protected readonly canGoDown = computed(
    () => this.hasCleanRun() && this.meldPoints() >= GoingDown,
  );

  /** The last thing this turn did, taken back. */
  protected undo(): void {
    this.moves.undo();
  }

  /** Rules 12.1, 12.2. */
  protected offerARedeal(): void {
    this.moves.offerARedeal();
  }

  /** Rule 10.6: the same saying-so the page makes when it opens. */
  protected stillHere(): void {
    void this.lobby.watch(this.tableId());
  }

  /** Rule 12.9, which only opens the choice of what to force. */
  protected chooseHowToForce(): void {
    this.asking.choosing.set(true);
  }

  /** Rule 12.10. */
  protected forceARedeal(atOnce: boolean): void {
    this.asking.choosing.set(false);
    this.moves.forceARedeal(atOnce);
  }

  /** Rule 9.2: a game of several hands, so where it stands is worth a column. */
  protected readonly across = computed(() => this.table()?.mode === 'survival');

  /**
   * Rules 8.3 and 9.4. A hand on its own is placed by what it came to; a
   * hand that is one of several is placed by where the game stands after
   * it, which is the question the next hand will be played on. Whoever is
   * out of it sits under the rest whatever they have.
   */
  protected readonly results = computed<readonly ResultRow[]>(() => {
    const hand = this.hand();
    const seats = this.seating.seats();

    if (!hand?.scores.length) {
      return [];
    }

    const standing = hand.standing;
    const played = Math.max(...standing.hands);
    const rows = hand.scores.map((score, seat) => ({
      place: hand.places[seat],
      name: seats[seat]?.displayName ?? '',
      score,
      won: seat === hand.winnerSeat,
      wentDown: hand.wentDown[seat],
      mine: seat === this.seating.ownSeat(),
      total: standing.totals[seat] ?? 0,
      hands: standing.hands[seat] ?? 0,
      out: standing.out[seat] ?? false,
      playedLast: (standing.hands[seat] ?? 0) === played,
      staying: hand.staying[seat] ?? null,
    }));

    return this.across() ? byStanding(rows) : rows.sort((one, other) => one.place - other.place);
  });

  /**
   * Rule 9.4 in the middle of a hand, where there is no hand to show yet.
   * How close somebody is to the number is how the hand in front of them
   * should be played, so it is worth asking for before it is over.
   */
  protected readonly standingRows = computed<readonly ResultRow[]>(() => {
    const hand = this.hand();
    const seats = this.seating.seats();

    if (!hand || !this.across()) {
      return [];
    }

    const { totals, hands, last, out } = hand.standing;
    const played = Math.max(...hands);

    return byStanding(
      seats.flatMap((who, seat) =>
        who.playerId
          ? [
              {
                place: 0,
                name: who.displayName ?? '',
                score: last[seat] ?? 0,
                won: false,
                wentDown: false,
                mine: seat === this.seating.ownSeat(),
                total: totals[seat] ?? 0,
                hands: hands[seat] ?? 0,
                out: out[seat] ?? false,
                playedLast: (hands[seat] ?? 0) === played,
                staying: null,
              },
            ]
          : [],
      ),
    );
  });

  /** Open only while somebody is reading it. */
  protected readonly reading = signal(false);

  /**
   * Which result is on the screen, counted in the hands behind it, and
   * nought before there is one.
   */
  private readonly resultNumber = computed(() => {
    const hands = this.hand()?.standing.hands ?? [];

    return hands.length > 0 ? Math.max(...hands) : 0;
  });

  /** The last result this player read and put away. */
  private readonly readAway = signal(0);

  /**
   * Whether the window has been put away. It says which result was put
   * away rather than that one was, because everything about a table
   * arrives again every time anybody else moves: a flag would be cleared
   * by somebody else pressing their own button, and the window would come
   * back in front of whoever had just closed it.
   */
  protected readonly done = computed(
    () => this.resultNumber() > 0 && this.readAway() === this.resultNumber(),
  );

  /**
   * Rule 9.6: out of the game, and this is the hand that did it. Asked once
   * and not again - a later hand finds them with fewer behind them than
   * whoever is still playing.
   */
  protected readonly justOut = computed(() => {
    const hand = this.hand();
    const seat = this.seating.ownSeat();

    if (!hand?.scores.length || seat === -1 || hand.staying.length > 0) {
      return false;
    }

    const { out, hands } = hand.standing;

    return out[seat] === true && hands[seat] === Math.max(...hands);
  });

  /** Rule 9.4: nobody is being asked anything, so a deal is what follows. */
  protected readonly anotherHand = computed(() => (this.hand()?.staying.length ?? 0) === 0);

  /** Rule 9.5: who the game went to, once there is a game to have been won. */
  protected readonly gameWinner = computed(() => {
    const seat = this.hand()?.standing.won ?? -1;

    return seat === -1 ? null : (this.seating.seats()[seat]?.displayName ?? '');
  });

  protected readonly wonTheGame = computed(
    () => (this.hand()?.standing.won ?? -1) === this.seating.ownSeat(),
  );

  /**
   * Read, and ready for the next. The window goes at once for whoever said
   * so; the table deals as soon as everybody watching has.
   */
  protected readTheHand(): void {
    this.readAway.set(this.resultNumber());
    this.moves.readTheHand();
  }

  /** Rule 9.6. */
  protected wentOn(leaving: boolean): void {
    if (!leaving) {
      this.readAway.set(this.resultNumber());

      return;
    }

    this.moves.giveUpTheChair();
    this.moves.backToLobby();
  }

  protected readonly winnerName = computed(() => {
    const hand = this.hand();

    return hand ? (this.seating.seats()[hand.winnerSeat]?.displayName ?? '') : '';
  });

  /** Rule 9.3: the table is asking and this player has still to answer. */
  protected readonly askedToStay = computed(() => {
    const staying = this.hand()?.staying ?? [];
    const seat = this.seating.ownSeat();

    return seat !== -1 && staying.length > 0 && staying[seat] === null;
  });

  /** Rule 9.3. */
  protected stay(another: boolean): void {
    this.moves.stay(another);

    // Rule 10.8 frees the chair once everybody has answered, which can be
    // a while yet. Nobody who has given it up should sit watching for it.
    if (!another) {
      this.moves.backToLobby();
    }
  }

  /** Rule 12.3. */
  protected answer(agreed: boolean): void {
    this.moves.answer(agreed);
  }

  /** Rule 12.4. */
  protected readTheResult(): void {
    this.asking.read.set(true);
    this.moves.readTheResult();
  }

  /** Rule 12.6. */
  protected keep(cardIds: readonly number[]): void {
    this.asking.chosen.set(true);
    this.moves.keep(cardIds);
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
}

/**
 * Rule 9.4: whoever is furthest from the number comes first, and whoever
 * is out of it sits under the rest whatever they have. The place is where
 * that order put them and not what any one hand came to.
 */
function byStanding(rows: readonly ResultRow[]): readonly ResultRow[] {
  return [...rows]
    .sort((one, other) => Number(one.out) - Number(other.out) || one.total - other.total)
    .map((row, at) => ({ ...row, place: at + 1 }));
}
