using Rummy51.Application.Games;
using Rummy51.Domain.Entities;
using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// What this table will leave behind. Nothing here decides anything or is
/// read by the game: it watches for the three moments worth keeping and
/// puts a description of each in a queue for somebody else to write down,
/// because a table must not wait on a database.
/// </summary>
internal sealed partial class OpenTable
{
    private readonly List<GameRecord> pending = [];

    private Guid gameId;
    private DateTimeOffset gameStartedAt;
    private string? dealtFinishingCard;
    private bool handWritten;

    /// <summary>Everything worth keeping since this was last asked.</summary>
    public IReadOnlyList<GameRecord> TakeRecords()
    {
        if (pending.Count == 0)
        {
            return [];
        }

        var taken = pending.ToArray();

        pending.Clear();

        return taken;
    }

    /// <summary>Rules 9.1 and 10.4: the game, and the players it will end with.</summary>
    private void NoteTheGameBegan()
    {
        gameId = Guid.CreateVersion7();
        gameStartedAt = clock.GetUtcNow();

        pending.Add(new GameBegan(
            gameId,
            Id,
            Mode,
            Limit,
            seats.Length,
            Jokers,
            [.. seats.Select((player, seat) => new SeatedPlayer(player!.PlayerId, seat))],
            gameStartedAt));
    }

    /// <summary>
    /// Rule 6.1 ends a hand on a throw, and every throw arrives here -
    /// a player's own and the one rule 3.6 makes for them.
    /// </summary>
    private void NoteIfTheHandEnded()
    {
        if (handWritten || hand is not { Phase: HandPhase.Finished } ended)
        {
            return;
        }

        handWritten = true;

        var at = clock.GetUtcNow();

        pending.Add(new HandEnded(
            gameId,
            Number: 1,
            ended.DealerSeat,
            ended.Deck.CutPosition ?? 0,
            dealtFinishingCard,
            ended.Deck.Seed,
            Codes(ended.Deck.Order),
            ended.AgreedRedeals,
            ended.ForcedRedeals,
            gameStartedAt,
            at,
            [.. Enumerable.Range(0, seats.Length).Select(seat => Scored(ended, seat))]));

        // Rule 9.2: one hand is the whole of a single game. The other two
        // modes end somewhere this does not reach yet.
        if (Mode == GameMode.Single)
        {
            pending.Add(new GameEnded(gameId, seats[ended.WinnerSeat]?.PlayerId, at));
        }
    }

    private PlayerResult Scored(Hand ended, int seat) =>
        new(
            seats[seat]!.PlayerId,
            seat == ended.WinnerSeat,
            ended.WentDown[seat],
            ended.WentRummy[seat],
            ended.TookTheFinishingCard[seat],
            gone[seat] is not null,
            ended.Hands[seat].Count,
            Codes(ended.Hands[seat].Select(card => card.Card)),
            Worth.Held(ended.Hands[seat].Select(card => card.Card)),
            Doubled: 1,
            ended.Scores[seat]);

    private static string Codes(IEnumerable<Card> cards) =>
        string.Join(',', cards.Select(card => card.Code));
}
