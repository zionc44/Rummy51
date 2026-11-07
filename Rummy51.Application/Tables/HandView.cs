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
    IReadOnlyList<string> Backs,
    IReadOnlyList<string> RevealedAtCut,
    string? FinishingCard);
