import { CardJourney } from './card-journey';
import { Crossings } from './crossings';
import { Dealing } from './dealing';
import { MeldJourney } from './meld-journey';
import { TableSeat } from './table-seat/table-seat';

/**
 * Every place on this screen that a card is seen to leave or arrive at.
 *
 * The page is the only thing that can answer any of these, because the
 * places are its own children and half of them are found by measuring. So
 * the page answers them and nothing else here knows what a component is.
 */
export interface Landings {
  fan(): readonly HTMLElement[];
  stock(): HTMLElement | undefined;
  discards(): HTMLElement | undefined;
  finishing(): HTMLElement | undefined;
  cardAt(id: number): HTMLElement | undefined;

  /** The nth place in the viewer's own hand, which is the only hand on screen. */
  inHand(step: number): HTMLElement | undefined;

  /** The place across the table at that seat, if it is one of them. */
  across(seat: number): TableSeat | undefined;

  /** A watcher has no seat of their own, so this is false for all of them. */
  mine(seat: number): boolean;

  nearSeat(): number;
  ownSeat(): number;

  /** The whole page, which is the one thing holding both ends of a crossing. */
  page(): HTMLElement;
}

/**
 * Everything on this table that is seen to travel: the deal going out, a
 * card between a pile and the hand, and a run crossing to the felt.
 *
 * The three of them are here together because they want the same answer -
 * where is that, on the screen, now - and because the page should be left
 * saying what the table is rather than what it is doing.
 */
export class Journeys {
  constructor(private readonly at: Landings) {}

  /** The cut and the deal played out on screen. */
  readonly deal = new Dealing({
    fanCards: () => this.at.fan(),
    pile: () => this.at.stock(),
    finishingSlot: () => this.at.finishing(),
    faceUp: (seat, step) => this.faceUpSpot(seat, step),
    dealt: (seat, step) =>
      this.at.mine(seat) ? this.at.inHand(step) : this.at.across(seat)?.pileSpot(),
  });

  /** One card's journey between a pile and the hand, drawn or thrown. */
  readonly card = new CardJourney({
    stock: () => this.at.stock(),
    discards: () => this.at.discards(),
    finishing: () => this.at.finishing(),
    cardAt: (id) => this.at.cardAt(id),
  });

  /**
   * Cards seen to cross between the hand and the felt, whichever way they
   * are going. It is given the whole page to look in, because that is the
   * only thing that holds both ends of the journey.
   */
  readonly crossing = new MeldJourney(() => this.at.page());

  /**
   * What crossed the table, flown. A throw of this player's own was already
   * sent from the card itself as they let go of it; everybody else's comes
   * out of a pile of backs, because that is all anyone was ever shown of it.
   */
  fly(what: Crossings): void {
    if (what.meld !== null) {
      this.crossing.crossed(what.meld.ids, this.pileAt(what.meld.seat));
    }

    if (what.thrownFrom !== null && what.thrownFrom !== this.at.ownSeat()) {
      this.card.threwFrom(this.pileAt(what.thrownFrom));
    }
  }

  /** The page is going. Nothing in the air outlives it. */
  land(): void {
    this.deal.stop();
    this.card.land();
    this.crossing.land();
  }

  /**
   * Where a card going face up at a seat comes to rest. The near place keeps
   * its cards in a hand of its own; every other seat keeps the few it shows
   * beside the picture.
   */
  private faceUpSpot(seat: number, step: number): HTMLElement | undefined {
    return seat === this.at.nearSeat()
      ? this.at.inHand(step)
      : this.at.across(seat)?.landingSpot(step);
  }

  /**
   * Where the cards of a hand nobody may see are drawn, which is a pile of
   * backs beside that player. The near seat shows no pile - its cards are
   * all there to be looked at - and needs none: they are found where they
   * lie.
   */
  private pileAt(seat: number): HTMLElement | undefined {
    return this.at.across(seat)?.backSpot();
  }
}
