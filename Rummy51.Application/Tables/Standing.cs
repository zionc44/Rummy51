namespace Rummy51.Application.Tables;

/// <summary>
/// Rules 9.4 and 9.5: the game as it stands across its hands, by seat.
/// How long each of them has lasted is the standing itself and not a
/// decoration - it is what places them when the game ends.
/// </summary>
/// <param name="Last">
/// What the hand before this one came to, kept because the standing is
/// worth reading in the middle of a hand and the hand it followed is gone
/// by then. Nought for a seat that was not in it.
/// </param>
/// <param name="Read">
/// Who has read what the hand came to and is waiting on nobody. A seat
/// answers for itself, so the window that showed it needs no memory of
/// its own and nothing to put back at the start of a game.
/// </param>
/// <param name="Won">
/// Rule 9.5: the seat the game itself went to, which is not the seat that
/// took any one hand. Minus one until there is a game to have been won.
/// </param>
public sealed record Standing(
    IReadOnlyList<int> Totals,
    IReadOnlyList<int> Hands,
    IReadOnlyList<int> Last,
    IReadOnlyList<bool> Out,
    IReadOnlyList<bool> Read,
    int Won);
