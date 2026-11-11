namespace Rummy51.Application.Tables;

/// <summary>
/// A joker bought out of a meld: which card is paying for it, which of the
/// melds it is lying in, and where in that meld it lies. The meld and the
/// place are named as everybody watching sees them.
/// </summary>
public sealed record SwapRequest
{
    public int CardId { get; init; }

    public int Meld { get; init; }

    public int At { get; init; }
}
