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
            Number: handsSoFar + 1,
            ended.DealerSeat,
            ended.Deck.CutPosition ?? 0,
            dealtFinishingCard,
            ended.Deck.Seed,
            Codes(ended.Deck.Order),
            ended.AgreedRedeals,
            ended.ForcedRedeals,
            gameStartedAt,
            at,
            // Rule 9.4: only the seats this hand was dealt to. A seat that
            // was out of it has nothing to be scored for, and a row of
            // nought is worse than no row - it reads as a hand they played
            // and drew. It can also be an empty chair by now, under 9.6.
            [
                .. Enumerable.Range(0, seats.Length)
                    .Where(seat => ended.Playing[seat])
                    .Select(seat => Scored(ended, seat))
            ],
            Asked(ended)));

    }

    /// <summary>Rules 9.2 and 9.5: whoever the mode's own ending left in front.</summary>
    private void NoteTheGameEnded()
    {
        var won = WonTheGame();

        pending.Add(new GameEnded(
            gameId,
            won == -1 ? null : seats[won]?.PlayerId,
            clock.GetUtcNow()));
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
            ended.ResultMultiplier,
            ended.Scores[seat]);

    /// <summary>Chapter 12, turned from seats into the people sitting in them.</summary>
    private IReadOnlyList<RedealAsked> Asked(Hand ended) =>
    [
        .. ended.Redeals.Select((redeal, before) => new RedealAsked(
            before + 1,
            seats[redeal.By]!.PlayerId,
            redeal.Forced,
            redeal.AtOnce,
            string.Join(',', redeal.ForcedWith),
            redeal.Agreed,
            [.. redeal.Replies.Select((reply, seat) => new RedealReplied(seats[seat]!.PlayerId, reply))],
            redeal.AskedAt,
            redeal.SettledAt))
    ];

    private static string Codes(IEnumerable<Card> cards) =>
        string.Join(',', cards.Select(card => card.Code));
}
