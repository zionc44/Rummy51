using Rummy51.Domain.Game;

namespace Rummy51.Application.Tables;

/// <summary>
/// The hand as everyone at the table may see it, which is chapter 11 and
/// nothing beyond it: the counts under 11.2, who is down and what is lying
/// there under 11.3, what the player in turn may do under 11.4, the deck
/// under 11.5 and the deal under 11.6, and the top of the pile under 3.3.
///
/// Cards are named by their code, which is also what the picture is drawn
/// from, so there is one name for a card and not two. The seats that mean
/// nobody - a turn before play begins, a winner before one is taken - are
/// -1 rather than null, so the client counts seats and never asks twice.
/// </summary>
/// <param name="JokerPositions">
/// Rule 11.5 says nobody may know this. It reaches a client only where a
/// development setting has asked for it, and knowing it is a cheat.
/// </param>
/// <summary>
/// Who the table is waiting for, and how much longer. A state and not an
/// instruction: a player who opens the page halfway through a wait is told
/// what is left of it, so their ring is everybody else's and not a fresh one.
/// </summary>
/// <param name="SecondsLeft">As of this message, counted down from.</param>
/// <param name="Seconds">The whole of it, so an arc can be a fraction.</param>
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
    int WinnerSeat,
    IReadOnlyList<bool> WentDown,
    bool HasDrawn,
    string? DiscardTop,
    int DiscardCount,
    IReadOnlyList<MeldView> Melds,
    bool CanUndo,
    bool CanAdd,
    bool CanTakeFinishing,
    Waiting? Waiting);
