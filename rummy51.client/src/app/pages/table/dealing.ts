import { computed, signal } from '@angular/core';
import { finishingPosition, jokersAtCut, revealedAt } from '../../core/cards/the-cut';
import { pictureOf } from '../../core/cards/deck';
import { Flight, betweenCentres } from '../../core/dom/flight';
import { DealPass, HandView } from '../../core/lobby/lobby.models';
import { Arrivals } from './arrivals';

/** Each card the cut turns up turns over where it lies, then is read. */
const Turn = 350;
const Read = 80;

/** Then the rest of the deck gathers: one card's journey, and the gap between two. */
const Travel = 240;
const Stagger = 3;

/** A card that leaves the fan - to the cutter, or to the finishing slot. */
const Place = 380;

/** The deal: a card's journey, the gap inside a pass, and between two. */
const Pass = 260;
const BetweenCards = 50;
const BetweenPasses = 150;

/**
 * Where on screen the things a card flies between are. Only the page that
 * draws them can say, so it is asked rather than searched for.
 */
export interface Spots {
  fanCards(): readonly HTMLElement[];
  pile(): HTMLElement | undefined;
  finishingSlot(): HTMLElement | undefined;

  /** Where a card going face up at a seat comes to rest, place by place. */
  faceUp(seat: number, step: number): HTMLElement | undefined;

  /**
   * Where a card being dealt to a seat comes to rest, which is a different
   * question: face up in your own hand, or onto the pile at anybody else's,
   * where nobody may look at it.
   */
  dealt(seat: number, step: number): HTMLElement | undefined;
}

/**
 * A hand being set up, played out in the order it happens at a table: every
 * joker the cutter turns over goes to them before the card beneath it is
 * touched; then the rest of the deck gathers itself into the pile; and last
 * of all the finishing card turns a quarter and takes its place beside it.
 * Then the deal, pass after pass round the table.
 *
 * Nothing here decides anything. The server has already dealt the hand and
 * this only shows it happening, which is why every number in it is a
 * duration and none of them is a card.
 */
export class Dealing {
  constructor(private readonly spots: Spots) {}

  /** True from the cut until the deck has settled, which keeps both on screen. */
  readonly collapsing = signal(false);

  /** True while the cards are going round the table. */
  readonly dealing = signal(false);

  /**
   * How many of the cards the cut turned up have been turned over so far.
   * They go one at a time: a joker is gone to the cutter before the card
   * beneath it is touched, which is the order a hand is really cut in.
   */
  readonly turned = signal(0);

  /** How much of the deal has reached each seat. */
  readonly arrivals = new Arrivals();

  /** While anything is moving, a seat shows only what has reached it. */
  readonly animating = computed(() => this.collapsing() || this.dealing());

  private readonly timers: number[] = [];
  private readonly flight = new Flight();
  private hand: HandView | null = null;

  /** Nothing has been dealt, which is where every hand starts. */
  forget(): void {
    this.arrivals.forget();
  }

  /**
   * The cut sweeping into the pile, and the deal behind it. Whoever turned
   * animation off in their system meant it here too: the deck simply is in
   * the pile, and the cards simply are at the seats.
   */
  begin(hand: HandView): void {
    this.hand = hand;

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return;
    }

    this.collapsing.set(true);

    // Two frames: one for the cards to be drawn face down before they are
    // asked to turn, and so the board exists to be measured against.
    requestAnimationFrame(() => {
      this.turned.set(1);

      requestAnimationFrame(() => {
        const cards = this.spots.fanCards();

        if (cards.length === 0) {
          this.endSweep();
          return;
        }

        this.scheduleTheCut(cards);
      });
    });
  }

  stop(): void {
    this.timers.forEach((timer) => window.clearTimeout(timer));
    this.flight.landAll();
  }

  /** From the first card already turning, laid end to end. */
  private scheduleTheCut(cards: readonly HTMLElement[]): void {
    const jokers = jokersAtCut(this.hand);
    const revealed = revealedAt(this.hand);
    const facedown = cards.filter((_, position) => !revealed.has(position));
    let at = Turn + Read;

    // Every joker's place at the seat is kept from the start, so each one
    // has somewhere measured to fly to when its turn comes.
    this.arrivals.expect(this.cutterSeat(), jokers.length);

    jokers.forEach((position, step) => {
      this.after(at, () => this.sendJokerToCutter(cards[position], step));
      at += Place;
      this.after(at, () => {
        this.arrivals.land(this.cutterSeat(), 1);
        this.turned.set(step + 2);
      });
      at += Turn + Read;
    });

    this.after(at, () => this.gatherToPile(facedown));
    at += facedown.length * Stagger + Travel;
    this.after(at, () => this.sendTheFinishingCard(cards));
    at += Place;
    this.after(at, () => this.endSweep());
  }

  /**
   * A joker at the cut is the cutter's, so it travels to the place waiting
   * for it at their seat and stays there, face up, for the whole table to
   * read until the deal is over.
   */
  private sendJokerToCutter(card: HTMLElement | undefined, step: number): void {
    const spot = this.spots.faceUp(this.cutterSeat(), step)?.getBoundingClientRect();

    if (spot === undefined || card === undefined) {
      return;
    }

    const from = card.getBoundingClientRect();
    const copy = this.copyOutOfTheFan(card, from, Place);

    // Down to the size of the place it is going to, which is cut to the
    // seat it belongs to and is not the size a card is in the fan.
    copy.style.transform = `${betweenCentres(from, spot)} scale(${spot.width / from.width})`;
  }

  /** The one it leaves behind keeps its place in the row, hidden. */
  private copyOutOfTheFan(card: HTMLElement, from: DOMRect, over: number): HTMLElement {
    const copy = this.flight.send(card.cloneNode(true) as HTMLElement, from);

    copy.style.transform = 'none';
    copy.style.transition = `transform ${over}ms ease-in-out`;
    card.style.visibility = 'hidden';

    return copy;
  }

  /** Everything still face down, from the far end of the deck back. */
  private gatherToPile(cards: readonly HTMLElement[]): void {
    const target = this.spots.pile()?.getBoundingClientRect();

    if (target === undefined) {
      return;
    }

    cards.forEach((card, index) => {
      const from = card.getBoundingClientRect();

      card.style.transitionDuration = `${Travel}ms`;
      card.style.transitionDelay = `${(cards.length - 1 - index) * Stagger}ms`;
      card.style.transform =
        `translate(${target.left - from.left}px, ${target.top - from.top}px)`;
    });
  }

  /** The last card left in the fan, and the only one still to be placed. */
  private sendTheFinishingCard(cards: readonly HTMLElement[]): void {
    const slot = this.spots.finishingSlot()?.getBoundingClientRect();
    const where = finishingPosition(this.hand);
    const card = where === null ? undefined : cards[where];

    if (slot === undefined || card === undefined) {
      return;
    }

    const from = card.getBoundingClientRect();

    card.style.zIndex = '20';
    card.style.transitionDuration = `${Place}ms`;
    card.style.transitionDelay = '0ms';
    card.style.transform = `${betweenCentres(from, slot)} rotate(90deg)`;
  }

  private endSweep(): void {
    // The deal follows the cut with nobody deciding anything in between,
    // here as on the server, and it starts before the fan is taken away so
    // there is no still moment between the two.
    this.dealTheHand();
    this.collapsing.set(false);
    this.turned.set(0);
    this.flight.landAll();
  }

  /**
   * The deal as the server counted it out: pass after pass round the table,
   * three cards at a time and fewer where the cut already gave somebody a
   * joker.
   */
  private dealTheHand(): void {
    const passes = this.hand?.passes ?? [];

    if (passes.length === 0) {
      return;
    }

    this.dealing.set(true);

    let at = 0;
    let dealt = 0;

    passes.forEach((pass) => {
      const before = dealt;

      this.after(at, () => this.flyPass(pass, before));
      at += BetweenPasses;
      dealt += pass.count;
    });

    // Clear of the last card landing, so no copy is taken away in the air.
    this.after(at + Pass + BetweenPasses * 2, () => this.endDeal());
  }

  /** One pass: a seat's three cards, one just behind the other. */
  private flyPass(pass: DealPass, dealt: number): void {
    this.arrivals.expect(pass.seat, pass.count);

    // Two frames, so the places the cards are going have been laid out
    // before anything measures them.
    requestAnimationFrame(() => requestAnimationFrame(() => this.sendPass(pass, dealt)));
  }

  /** The cards of one pass put in the air, each a little behind the last. */
  private sendPass(pass: DealPass, dealt: number): void {
    const from = this.spots.pile()?.getBoundingClientRect();
    const landed = this.arrivals.reached(pass.seat);
    const sent: HTMLElement[] = [];

    for (let step = 0; step < pass.count; step++) {
      const to = this.spots.dealt(pass.seat, landed + step)?.getBoundingClientRect();

      if (from !== undefined && to !== undefined) {
        sent.push(this.flyCard(this.backOfDealt(dealt + step), from, to, step * BetweenCards));
      }
    }

    this.after(Pass + pass.count * BetweenCards, () => this.landPass(pass, sent));
  }

  /** They are there, so the copies that stood in for them can go. */
  private landPass(pass: DealPass, sent: readonly HTMLElement[]): void {
    this.arrivals.land(pass.seat, pass.count);

    // A frame later, so the card it is standing in for is drawn first.
    requestAnimationFrame(() => sent.forEach((card) => card.remove()));
  }

  /**
   * The back the next card off the deck wears. The deal starts just past the
   * finishing card and runs on round the deck, so each one keeps the back of
   * the pack it came from rather than being given one.
   */
  private backOfDealt(step: number): string {
    const hand = this.hand;

    if (hand === null || hand.cutAt === null) {
      return '1B';
    }

    return hand.backs[(hand.cutAt + hand.revealedAtCut.length + 1 + step) % hand.deckSize];
  }

  /** A card on its way, which is a back and a journey and nothing else. */
  private flyCard(back: string, from: DOMRect, to: DOMRect, delay: number): HTMLElement {
    const card = document.createElement('img');

    card.src = pictureOf(back);
    card.alt = '';

    this.flight.send(card, from);

    card.style.zIndex = '30';
    card.style.transition = `transform ${Pass}ms ease-in-out ${delay}ms`;
    card.style.transform = `${betweenCentres(from, to)} scale(${to.width / from.width})`;

    return card;
  }

  private endDeal(): void {
    this.dealing.set(false);
    this.flight.landAll();
  }

  private cutterSeat(): number {
    return this.hand?.cutterSeat ?? -1;
  }

  private after(delay: number, run: () => void): void {
    this.timers.push(window.setTimeout(run, delay));
  }

}
