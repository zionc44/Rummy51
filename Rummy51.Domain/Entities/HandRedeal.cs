namespace Rummy51.Domain.Entities;

/// <summary>
/// One redeal put to the table, whether or not it was taken. It hangs on
/// the hand and not on the player because rules 12.2 and 12.8 let a single
/// hand hold several, so what somebody said is a fact about the asking.
/// </summary>
public class HandRedeal
{
    public Guid Id { get; set; }

    public Guid PlayedHandId { get; set; }

    /// <summary>First is one, in the order they were put.</summary>
    public int Number { get; set; }

    /// <summary>Rules 12.1 and 12.9.</summary>
    public Guid OfferedById { get; set; }

    /// <summary>Rule 12.9.</summary>
    public bool Forced { get; set; }

    /// <summary>Rule 12.10.</summary>
    public bool AtOnce { get; set; }

    /// <summary>Rule 12.9's pairs by their codes, and empty where it was not forced.</summary>
    public required string ForcedWith { get; set; }

    /// <summary>Rule 12.3, and not whether it was dealt again: rule 12.11 does that either way.</summary>
    public bool Agreed { get; set; }

    public DateTimeOffset AskedAt { get; set; }

    public DateTimeOffset SettledAt { get; set; }
}
