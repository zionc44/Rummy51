namespace Rummy51.Application.Tables;

/// <summary>
/// Rules 9.4 and 9.5: the game as it stands across its hands, by seat.
/// How long each of them has lasted is the standing itself and not a
/// decoration - it is what places them when the game ends.
/// </summary>
/// <param name="EachHand">
/// Rule 9.2: what every hand came to, a row to a hand and a seat to a
/// column. Null where a seat was not in that hand, a nought being a hand
/// played and scored nothing in. The running total under a cell is the
/// column down to it, and is worked out where it is shown.
/// </param>
/// <param name="Read">
/// Who has read what the hand came to and is waiting on nobody. A seat
/// answers for itself, so the window that showed it needs no memory of
/// its own and nothing to put back at the start of a game.
/// </param>
/// <param name="Places">
/// Rule 9.5, and nought for a chair that was never in the game. It is
/// worked out here and not on the screen, because a placing worked out
/// twice is a placing that will be changed once.
/// </param>
/// <param name="Won">
/// Rule 9.5: the seat the game itself went to, which is not the seat that
/// took any one hand. Minus one until there is a game to have been won.
/// </param>
public sealed record Standing(
    IReadOnlyList<int> Totals,
    IReadOnlyList<int> Hands,
    IReadOnlyList<IReadOnlyList<int?>> EachHand,
    IReadOnlyList<bool> Out,
    IReadOnlyList<bool> Read,
    IReadOnlyList<int> Places,
    int Won);
