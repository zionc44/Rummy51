namespace Rummy51.Domain.Entities;

/// <summary>
/// What one hand came to for one player.
///
/// <see cref="ResultMultiplier"/> and <see cref="Score"/> are written as they
/// were applied rather than worked out again later. Rules 8.1 and 8.2 may
/// yet change, and a hand already played must keep the score it was played
/// for; everything beside them is the evidence it was arrived at from.
/// </summary>
public class HandResult
{
    public Guid PlayedHandId { get; set; }

    public Guid PlayerId { get; set; }

    /// <summary>Rule 6.1.</summary>
    public bool Won { get; set; }

    /// <summary>Rule 8.1 hangs its hundred on this.</summary>
    public bool WentDown { get; set; }

    /// <summary>Rule 6.6.</summary>
    public bool WentRummy { get; set; }

    /// <summary>Rule 7.1.</summary>
    public bool TookFinishingCard { get; set; }

    /// <summary>Rule 10.1.</summary>
    public bool WalkedAway { get; set; }

    public int CardsLeft { get; set; }

    /// <summary>By their codes, so the hand can be read back and not only counted.</summary>
    public required string HeldCards { get; set; }

    /// <summary>Rules 1.2 and 1.3, before rule 8.1 reads it.</summary>
    public int HeldWorth { get; set; }

    /// <summary>Rule 8.2: one, two or four, and one is not a doubling.</summary>
    public int ResultMultiplier { get; set; }

    /// <summary>Rule 8.1, signed, so the winner's is negative.</summary>
    public int Score { get; set; }
}
