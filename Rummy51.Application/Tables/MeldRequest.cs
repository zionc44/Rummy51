namespace Rummy51.Application.Tables;

/// <summary>
/// The cards being laid on the table, by the numbers they were given when
/// they reached the hand - the same way a card is named when it is thrown,
/// and for the same reason.
/// </summary>
public sealed record MeldRequest
{
    public IReadOnlyList<int> CardIds { get; init; } = [];
}
