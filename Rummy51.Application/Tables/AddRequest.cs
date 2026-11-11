using Rummy51.Domain.Game;

namespace Rummy51.Application.Tables;

/// <summary>
/// A card added to a meld already lying on the table: which card, which of
/// the melds, and which end of it. The meld is named by its place on the
/// table, which is the same place for everybody watching.
/// </summary>
public sealed record AddRequest
{
    public int CardId { get; init; }

    public int Meld { get; init; }

    public MeldEnd End { get; init; }
}
