namespace Rummy51.Application.Tables;

/// <summary>By the number it was given, as <see cref="Domain.Game.HeldCard"/> says.</summary>
public sealed record DiscardRequest
{
    public int CardId { get; init; }
}
