namespace Rummy51.Application.Tables;

/// <summary>Where in the deck the cutter chose to cut, counted from the top.</summary>
public sealed record CutRequest
{
    public int Position { get; init; }
}
