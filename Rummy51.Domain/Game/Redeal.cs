using Rummy51.Domain.Entities;

namespace Rummy51.Domain.Game;

/// <summary>
/// A redeal the hand has finished with, kept so that it can say what was
/// asked of it and not only how many times. Seats and not players: who is
/// sitting where is the table's business and never the hand's.
/// </summary>
public sealed record Redeal(
    int By,
    bool Forced,
    bool AtOnce,
    IReadOnlyList<string> ForcedWith,
    bool Agreed,
    IReadOnlyList<RedealReply> Replies,
    DateTimeOffset AskedAt,
    DateTimeOffset SettledAt);
