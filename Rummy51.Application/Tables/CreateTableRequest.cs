using Rummy51.Domain.Entities;

namespace Rummy51.Application.Tables;

public sealed record CreateTableRequest
{
    public int Seats { get; init; }

    public GameMode Mode { get; init; }

    /// <summary>
    /// Points that end a survival game, or hands in a series. Null for a
    /// single hand, which asks for nothing further.
    /// </summary>
    public int? Limit { get; init; }
}
