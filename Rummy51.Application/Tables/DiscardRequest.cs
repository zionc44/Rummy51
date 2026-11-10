namespace Rummy51.Application.Tables;

/// <summary>
/// The card being thrown away, by the number it was given when it reached the
/// hand. Not by what it is, since two sevens of hearts are the same card and
/// a player throwing one of them means that one; and not by where it sits,
/// since the order a hand is held in is the player's own and never crosses
/// the wire.
/// </summary>
public sealed record DiscardRequest
{
    public int CardId { get; init; }
}
