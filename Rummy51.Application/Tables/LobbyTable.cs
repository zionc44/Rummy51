using Rummy51.Domain.Entities;

namespace Rummy51.Application.Tables;

/// <summary>
/// One place at a table. Empty is a seat whose player is null; away is one
/// whose player is not watching, which rule 10.2 makes one thing. At a real
/// table you can see that somebody got up.
/// </summary>
public sealed record TableSeat(Guid? PlayerId, string? DisplayName, string? AvatarUrl, bool Away);

/// <summary>
/// A table as everyone sees it. The seats keep their order: whoever opened the
/// table took the first, and someone leaving empties a seat rather than moving
/// the rest along.
/// </summary>
public sealed record LobbyTable(
    Guid TableId,
    GameMode Mode,
    int? Limit,
    int Jokers,
    IReadOnlyList<TableSeat> Seats,
    bool Started);
