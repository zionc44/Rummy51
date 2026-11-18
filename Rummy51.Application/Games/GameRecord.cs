using Rummy51.Domain.Entities;

namespace Rummy51.Application.Games;

/// <summary>
/// Something that happened at a table and outlives it. A table is memory
/// and a game is not: these are the three moments worth writing down, and
/// they are plain descriptions rather than rows, so that whoever writes
/// them can be replaced without the game noticing.
/// </summary>
public abstract record GameRecord;

/// <summary>Rules 9.1, 9.2 and 10.4: it begins with the players it will end with.</summary>
public sealed record GameBegan(
    Guid GameId,
    Guid TableId,
    GameMode Mode,
    int? Target,
    int Seats,
    int Jokers,
    IReadOnlyList<SeatedPlayer> Players,
    DateTimeOffset At) : GameRecord;

public sealed record SeatedPlayer(Guid PlayerId, int Seat);

/// <summary>
/// A hand played to its end, and what rule 8.1 made of it for each player.
/// The deal described is the one that was played out: rule 12.6 may have
/// thrown several away first, and those are counted rather than kept.
/// </summary>
public sealed record HandEnded(
    Guid GameId,
    int Number,
    int DealerSeat,
    int CutAt,
    string? FinishingCard,
    int Seed,
    string DeckOrder,
    int AgreedRedeals,
    int ForcedRedeals,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    IReadOnlyList<PlayerResult> Results) : GameRecord;

/// <summary>
/// Rules 8.1 and 8.2 as they were applied. <paramref name="Doubled"/> is
/// one until 8.2 is enforced, which is the truth and not a placeholder:
/// nothing doubles anything yet.
/// </summary>
public sealed record PlayerResult(
    Guid PlayerId,
    bool Won,
    bool WentDown,
    bool WentRummy,
    bool TookFinishingCard,
    bool WalkedAway,
    int CardsLeft,
    string HeldCards,
    int HeldWorth,
    int Doubled,
    int Score);

/// <summary>Rule 9.2: whichever of the three endings arrived.</summary>
public sealed record GameEnded(Guid GameId, Guid? WinnerId, DateTimeOffset At) : GameRecord;
