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
public sealed record HandView(
    HandPhase Phase,
    int DealerSeat,
    int CutterSeat,
    int DeckSize,
    int? CutAt,
    IReadOnlyList<string> Backs,
    IReadOnlyList<int> JokerPositions,
    IReadOnlyList<string> RevealedAtCut,
    string? FinishingCard);
