using Rummy51.Domain.Entities;
using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// Rules 8.4 and 8.5: what a series makes of each of its hands. The hand
/// is told its weight as it is dealt and scores itself by it; which hand
/// of the series it is, and what earlier redeals sent onto it, is here.
/// </summary>
internal sealed partial class OpenTable
{
    /// <summary>By hand, because rule 8.5 places a redeal from the hand it was agreed in.</summary>
    private readonly List<int> agreedRedeals = [];

    /// <summary>Every hand of a series, those still to come included, and nothing for any other game.</summary>
    public IReadOnlyList<HandWeight> Weights =>
        Mode == GameMode.Series && Limit is { } hands
            ? Series.Weights(hands, [.. agreedRedeals, .. StillCounting()])
            : [];

    /// <summary>The hand being played, until it has been counted into the game.</summary>
    private IEnumerable<int> StillCounting() =>
        hand is not null && !handWritten ? [hand.AgreedRedeals] : [];

    /// <summary>Asked before the hand exists, so it is the next one along.</summary>
    private HandWeight NextWeight() =>
        Weights.ElementAtOrDefault(handsSoFar) ?? HandWeight.Single;
}
