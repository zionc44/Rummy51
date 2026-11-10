using Rummy51.Domain.Game;

namespace Rummy51.Application.Tables;

/// <summary>Which of the two piles the player reached for.</summary>
public sealed record DrawRequest
{
    public DrawnFrom From { get; init; }
}
