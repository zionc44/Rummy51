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

    /// <summary>Rule 3.5, one restock to a line, and null where there was none.</summary>
    string? Restocks,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    IReadOnlyList<PlayerResult> Results,
    IReadOnlyList<RedealAsked> Redeals) : GameRecord;

/// <summary>
/// Chapter 12: one redeal put to the table and what came of it. A hand may
/// hold several, so they are numbered rather than counted.
/// </summary>
public sealed record RedealAsked(
    int Number,
    Guid OfferedById,
    bool Forced,
    bool AtOnce,
    string ForcedWith,
    bool Agreed,
    IReadOnlyList<RedealReplied> Answers,
    DateTimeOffset AskedAt,
    DateTimeOffset SettledAt);

public sealed record RedealReplied(Guid PlayerId, RedealReply Reply);

/// <summary>
/// Rules 8.1 and 8.2 as they were applied, and not as they would be
/// worked out today: a rule that changes must not rewrite a hand already
/// played.
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
    int ResultMultiplier,
    int Score);

/// <summary>Rule 9.2: whichever of the three endings arrived.</summary>
public sealed record GameEnded(
    Guid GameId,
    Guid? WinnerId,
    IReadOnlyList<PlayerStanding> Standing,
    DateTimeOffset At) : GameRecord;

/// <summary>
/// Rules 9.4 and 9.5: where the game left one player. It covers whoever
/// was in it and not whoever is still sitting there, because 9.6 empties
/// the chair of somebody who is out while the game plays on without them.
/// </summary>
public sealed record PlayerStanding(
    Guid PlayerId,
    int HandsPlayed,
    int Total,
    int? OutAfterHand,
    int Place);
