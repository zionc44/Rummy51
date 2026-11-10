using Rummy51.Domain.Game;

namespace Rummy51.Application.Tables;

/// <summary>
/// The hand as everyone at the table may see it. Cards are named by their
/// code, which is also what the picture is drawn from, so there is one name
/// for a card and not two.
/// </summary>
/// <param name="DeckSize">
/// How many cards are face down for the cutter to choose from. The cards
/// themselves are nobody's business: they are face down.
/// </param>
/// <param name="CutAt">
/// Where the cutter chose, as a position in the deck they were shown. Null
/// until they have chosen.
/// </param>
/// <param name="JokerPositions">
/// Where the jokers are lying in the deck, so a cut onto one can be aimed
/// at while the cut is being worked on. Empty unless a development setting
/// has asked for them, because knowing it is a cheat.
/// </param>
/// <param name="Backs">
/// The back each of those cards wears, by position. It says nothing about
/// the card underneath; a deck of one colour just looks like a printer's
/// mistake.
/// </param>
/// <param name="CardsPerSeat">
/// How many cards each seat is holding. Public: at a real table anybody can
/// count them, and the backs have to be drawn somewhere.
/// </param>
/// <param name="Passes">
/// The deal pass by pass, so the eye can follow it round the table. It says
/// how many cards went where and never which.
/// </param>
/// <param name="TurnSeat">Whose turn it is, or -1 before play has begun.</param>
/// <param name="HasDrawn">
/// Whether they have taken their card yet. Public, because at a real table
/// everybody watches the hand go to the stock and come back.
/// </param>
/// <param name="DiscardTop">
/// The one card of the discard pile lying face up, which is also the only one
/// anybody may take. Null until the hand's first discard.
/// </param>
/// <param name="DiscardCount">
/// How deep the pile is. Nobody plays against it, but a pile drawn the same
/// height all game says the game is not happening.
/// </param>
/// <summary>
/// Who the table is waiting for, and how much longer it will wait. It is a
/// state and not an instruction: a player who opens the page halfway
/// through a wait is told what is left of it, so their ring is the same as
/// everybody else's rather than a fresh one of their own.
/// </summary>
/// <param name="SecondsLeft">As of this message, which is what the client counts down from.</param>
/// <param name="Seconds">The whole of the wait, so an arc can be drawn as a fraction of it.</param>
public sealed record Waiting(int Seat, double SecondsLeft, double Seconds);

public sealed record HandView(
    HandPhase Phase,
    int DealerSeat,
    int CutterSeat,
    int DeckSize,
    int? CutAt,
    IReadOnlyList<string> Backs,
    IReadOnlyList<int> JokerPositions,
    IReadOnlyList<string> RevealedAtCut,
    string? FinishingCard,
    IReadOnlyList<int> CardsPerSeat,
    IReadOnlyList<DealPass> Passes,
    int StockSize,
    int TurnSeat,
    bool HasDrawn,
    string? DiscardTop,
    int DiscardCount,
    IReadOnlyList<MeldView> Melds,
    bool CanUndo,
    Waiting? Waiting);
