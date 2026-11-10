namespace Rummy51.Domain.Game;

/// <summary>
/// Cards laid on the table, and whose they were. Who laid them is not kept
/// for the look of it: only a player who has gone down may add to what is
/// lying there, and the score at the end of a hand asks the same question.
///
/// A card keeps the number it was given in the hand it came from. Nothing
/// needs it there any more, but it is what the screen follows a card by,
/// and a card that changes its name halfway across the table is a card the
/// screen has to draw twice.
/// </summary>
public sealed record Meld(int Seat, IReadOnlyList<HeldCard> Cards);
