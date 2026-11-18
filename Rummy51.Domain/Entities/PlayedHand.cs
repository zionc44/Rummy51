namespace Rummy51.Domain.Entities;

/// <summary>
/// One deal, which rule 9.1 calls a hand. Named for having been played
/// because <c>Hand</c> is the one being played, over in the game.
///
/// All of it is about the cards and none of it about people: who sat where
/// is the game's, and what each of them got is a result.
/// </summary>
public class PlayedHand
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    /// <summary>First is one, and rule 2.1 moves the deal on with each.</summary>
    public int Number { get; set; }

    public int DealerSeat { get; set; }

    /// <summary>Rule 2.2.</summary>
    public int CutAt { get; set; }

    /// <summary>Rule 2.2, by its code.</summary>
    public string? FinishingCard { get; set; }

    /// <summary>
    /// The shuffle as a recipe. It reproduces the deck only while the
    /// shuffle and the deck's starting order are what they were, and rule
    /// 12.6 puts it out of reach anyway: a redeal builds from what nobody
    /// kept, and no seed reaches that. Which is why the order is kept too.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>The shuffle as it came out, by card codes in order.</summary>
    public required string DeckOrder { get; set; }

    /// <summary>Rule 8.2 doubles on these.</summary>
    public int AgreedRedeals { get; set; }

    /// <summary>Rule 12.9's, which do not.</summary>
    public int ForcedRedeals { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }
}
