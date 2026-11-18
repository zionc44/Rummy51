using Microsoft.EntityFrameworkCore;
using Rummy51.Application.Games;
using Rummy51.Domain.Entities;
using Rummy51.Infrastructure.Persistence;

namespace Rummy51.Infrastructure.Games;

/// <summary>
/// The records as rows. A hand and its results go down together or not at
/// all: a hand with nobody's score against it, or a score against no hand,
/// is worse than neither.
/// </summary>
public sealed class GameRecords(IDbContextFactory<Rummy51DbContext> contexts) : IGameRecords
{
    public async Task WriteAsync(IReadOnlyList<GameRecord> records, CancellationToken stopping)
    {
        if (records.Count == 0)
        {
            return;
        }

        await using var db = await contexts.CreateDbContextAsync(stopping);
        await using var written = await db.Database.BeginTransactionAsync(stopping);

        foreach (var record in records)
        {
            Write(db, record);
        }

        await db.SaveChangesAsync(stopping);
        await written.CommitAsync(stopping);
    }

    private static void Write(Rummy51DbContext db, GameRecord record)
    {
        switch (record)
        {
            case GameBegan began:
                Began(db, began);
                break;

            case HandEnded ended:
                Ended(db, ended);
                break;

            case GameEnded over:
                Over(db, over);
                break;
        }
    }

    private static void Began(Rummy51DbContext db, GameBegan began)
    {
        db.Games.Add(new Game
        {
            Id = began.GameId,
            TableId = began.TableId,
            Mode = began.Mode,
            Target = began.Target,
            Seats = began.Seats,
            Jokers = began.Jokers,
            StartedAt = began.At,
        });

        db.GamePlayers.AddRange(began.Players.Select(player => new GamePlayer
        {
            GameId = began.GameId,
            PlayerId = player.PlayerId,
            Seat = player.Seat,
        }));
    }

    private static void Ended(Rummy51DbContext db, HandEnded ended)
    {
        var hand = new PlayedHand
        {
            Id = Guid.CreateVersion7(),
            GameId = ended.GameId,
            Number = ended.Number,
            DealerSeat = ended.DealerSeat,
            CutAt = ended.CutAt,
            FinishingCard = ended.FinishingCard,
            Seed = ended.Seed,
            DeckOrder = ended.DeckOrder,
            AgreedRedeals = ended.AgreedRedeals,
            ForcedRedeals = ended.ForcedRedeals,
            StartedAt = ended.StartedAt,
            EndedAt = ended.EndedAt,
        };

        db.PlayedHands.Add(hand);

        db.HandResults.AddRange(ended.Results.Select(result => new HandResult
        {
            PlayedHandId = hand.Id,
            PlayerId = result.PlayerId,
            Won = result.Won,
            WentDown = result.WentDown,
            WentRummy = result.WentRummy,
            TookFinishingCard = result.TookFinishingCard,
            WalkedAway = result.WalkedAway,
            CardsLeft = result.CardsLeft,
            HeldCards = result.HeldCards,
            HeldWorth = result.HeldWorth,
            Doubled = result.Doubled,
            Score = result.Score,
        }));
    }

    private static void Over(Rummy51DbContext db, GameEnded over)
    {
        // Attached rather than loaded: the row is known to be there, and
        // reading it back to write two columns would be a round trip for
        // nothing.
        var game = new Game { Id = over.GameId, WinnerId = over.WinnerId, FinishedAt = over.At };

        db.Games.Attach(game);
        db.Entry(game).Property(g => g.WinnerId).IsModified = true;
        db.Entry(game).Property(g => g.FinishedAt).IsModified = true;
    }
}
