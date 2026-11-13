namespace Rummy51.Application.Tables;

/// <summary>By their numbers, as <see cref="DiscardRequest"/> is.</summary>
public sealed record MeldRequest
{
    public IReadOnlyList<int> CardIds { get; init; } = [];
}
