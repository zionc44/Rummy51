using Rummy51.Domain.Game;

namespace Rummy51.Application.Tables;

/// <summary>
/// Who the table is waiting for, and how much longer. A state and not an
/// instruction: a player who opens the page halfway through a wait is told
/// what is left of it, so their ring is everybody else's and not a fresh one.
/// </summary>
/// <param name="SecondsLeft">As of this message, counted down from.</param>
/// <param name="Seconds">The whole of it, so an arc can be a fraction.</param>
public sealed record Waiting(int Seat, double SecondsLeft, double Seconds);

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
/// <param name="OfferedBy">
/// Who has put a redeal to the table under rule 12.1, or -1 when nobody
/// has. What each seat may see of the answers is not here: rule 12.4 shows
/// a different thing to each of them, so it goes where the cards go.
/// </param>
/// <param name="ForcedWith">
/// Rule 12.11: the four pairs a redeal was forced with, and empty until the
/// table may see them. Who *could* force is not here and never is - that is
/// rule 11.1, and it goes where cards go.
/// </param>
/// <param name="JokerPositions">
/// Rule 11.5 says nobody may know this. It reaches a client only where a
/// development setting has asked for it, and knowing it is a cheat.
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
    bool CanOfferRedeal,
    int OfferedBy,
    bool Forced,
    IReadOnlyList<string> ForcedWith,
    Waiting? Waiting);
