import { Signal, computed } from '@angular/core';
import { HeldCard } from '../../core/cards/card';
import { GoingDown, hasCleanRun, showingWorth } from '../../core/cards/melds';
import { NeverWentDown, worthOfHand } from '../../core/cards/worth';
import { HandView } from '../../core/lobby/lobby.models';

/**
 * What the hand in front of this player comes to, and where the card they
 * have picked out could go.
 *
 * It reads the hand as its owner has arranged it, which is why it can
 * answer at all: an arrangement never crosses the wire, so there is nobody
 * to ask. That is the whole reason `melds.ts` and `worth.ts` are written a
 * second time, and this is what reads them.
 */
export class Reading {
  constructor(
    private readonly hand: Signal<HandView | null>,
    private readonly cards: Signal<readonly HeldCard[]>,
    private readonly picked: Signal<number | null>,
    private readonly nearSeat: Signal<number>,
    private readonly laid: () => readonly (readonly HeldCard[])[],
  ) {}

  /** The runs and sets this hand is lying in, as its owner has laid them. */
  private readonly inMelds = computed(() => this.laid());

  /** What the melds come to, counted the way the fifty-one counts. */
  readonly meldPoints = computed(() => showingWorth(this.inMelds()));

  readonly hasCleanRun = computed(() => this.inMelds().some(hasCleanRun));

  readonly canGoDown = computed(() => this.hasCleanRun() && this.meldPoints() >= GoingDown);

  /** Whether this player has shown a hand, which the whole table may see. */
  readonly wentDown = computed(() => this.hand()?.wentDown[this.nearSeat()] ?? false);

  /**
   * What this hand costs if the hand ends now. A flat hundred until its
   * owner has gone down, whatever they are holding.
   */
  readonly worth = computed(() =>
    this.wentDown() ? worthOfHand(this.cards()) : NeverWentDown,
  );

  /**
   * Where the card the player has picked out could join what is lying on
   * the table. The places are the server's answer, carried on the card
   * itself, so the screen offers them without knowing the rule.
   */
  readonly fits = computed(() => this.thePicked()?.fits ?? []);

  /** And the jokers it is standing in for, which it could buy out. */
  readonly buys = computed(() => this.thePicked()?.buys ?? []);

  private readonly thePicked = computed(() => {
    const picked = this.picked();

    return this.cards().find((card) => card.id === picked);
  });
}
