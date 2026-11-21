using Rummy51.Domain.Entities;
using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// Rules 9.1 to 9.6: the game a hand belongs to. A hand knows nothing of
/// it - it is dealt, played and scored the same way whichever game it is
/// one of - so what runs across hands is counted here instead.
/// </summary>
internal sealed partial class OpenTable
{
    /// <summary>Rule 8.1 added up across the game, by seat.</summary>
    private readonly int[] totals;

    /// <summary>Rule 9.5: how long each of them lasted, which is the standing.</summary>
    private readonly int[] handsPlayed;

    /// <summary>Rule 9.4.</summary>
    private readonly bool[] outOfIt;

    /// <summary>What the hand before this one came to, which outlives it.</summary>
    private readonly int[] lastHand;

    /// <summary>Who has read what the hand came to and is ready for the next.</summary>
    private readonly bool[] readIt;

    private int handsSoFar;

    /// <summary>
    /// A hand is over and the next one is waiting on nothing but the table
    /// reading what it came to. Rule 9.4 ends a hand and begins a hand; the
    /// pause between them is this and not a rule.
    /// </summary>
    private bool showing;

    public IReadOnlyList<int> Totals => totals;

    public IReadOnlyList<int> HandsPlayed => handsPlayed;

    public IReadOnlyList<bool> OutOfIt => outOfIt;

    public IReadOnlyList<int> LastHand => lastHand;

    public IReadOnlyList<bool> ReadIt => readIt;

    /// <summary>Rule 9.5, and -1 until there is a game to have been won.</summary>
    public int WonSeat => staying is null ? -1 : WonTheGame();

    /// <summary>Who the next hand is dealt to, which rule 9.4 thins as it goes.</summary>
    private IReadOnlyList<bool> StillIn =>
        [.. Enumerable.Range(0, seats.Length).Select(seat => !outOfIt[seat])];

    /// <summary>
    /// Rule 9.4, and rule 9.2 for the other two: a single hand is the whole
    /// of its game, and a series counts hands that nothing counts yet, so
    /// for now it ends the same way.
    /// </summary>
    private bool TheGameIsOver =>
        Mode != GameMode.Survival || StillIn.Count(one => one) <= 1;

    /// <summary>
    /// Everything that happens because a hand finished, in the order it has
    /// to happen: counted before anybody is knocked out by the count, and
    /// knocked out before the table asks what comes next.
    /// </summary>
    private void TheHandEnded()
    {
        if (handWritten || hand is not { Phase: HandPhase.Finished } ended)
        {
            return;
        }

        NoteIfTheHandEnded();
        Count(ended);
        KnockOutWhoeverCrossed();

        if (!TheGameIsOver)
        {
            showing = true;
            Wait(turns.BetweenHandsSeconds);

            return;
        }

        NoteTheGameEnded();
        AskWhoIsStaying();
    }

    /// <summary>
    /// Rule 2.1 among those still in it, which rule 9.4 says steps over the
    /// rest the way it steps over an empty chair.
    /// </summary>
    private bool DealTheNextHand()
    {
        if (!showing)
        {
            return false;
        }

        showing = false;
        Array.Clear(readIt);
        Deal(Dealing.After(dealerWas, StillIn));

        return true;
    }

    /// <summary>
    /// Read, and ready for the next hand. The wait between hands is for
    /// reading and nothing else, so a table that has all read it has
    /// nothing left to wait for.
    /// </summary>
    public bool ReadTheHand(int seat)
    {
        if (!showing || seat < 0 || seat >= seats.Length || seats[seat] is null)
        {
            return false;
        }

        readIt[seat] = true;

        // Whoever is not watching cannot press anything, and the clock is
        // what covers them.
        if (Enumerable.Range(0, seats.Length).All(one => !Watching(one) || readIt[one]))
        {
            DealTheNextHand();
        }

        return true;
    }

    private bool Watching(int seat) => seats[seat] is not null && here[seat];

    /// <summary>Rule 9.6: out of the game, and giving the chair up with it.</summary>
    public bool GiveUpTheChair(int seat)
    {
        if (seat < 0 || seat >= seats.Length || !outOfIt[seat] || seats[seat] is null)
        {
            return false;
        }

        GiveUp(seat);

        return true;
    }

    /// <summary>Rule 8.1 into the running total, for the seats that played.</summary>
    private void Count(Hand ended)
    {
        handsSoFar += 1;

        for (var seat = 0; seat < seats.Length; seat++)
        {
            lastHand[seat] = ended.Playing[seat] ? ended.Scores[seat] : 0;

            if (!ended.Playing[seat])
            {
                continue;
            }

            totals[seat] += ended.Scores[seat];
            handsPlayed[seat] += 1;
        }
    }

    /// <summary>Rule 9.4: reaching the number is enough, and it is the end of this hand.</summary>
    private void KnockOutWhoeverCrossed()
    {
        if (Mode != GameMode.Survival || Limit is not { } limit)
        {
            return;
        }

        for (var seat = 0; seat < seats.Length; seat++)
        {
            outOfIt[seat] = outOfIt[seat] || totals[seat] >= limit;
        }
    }

    /// <summary>
    /// Rule 9.5: the last one in, or where everybody went out together, the
    /// lowest of them. A game of one hand has the hand's winner instead -
    /// there is nobody to be the last one left in it.
    /// </summary>
    private int WonTheGame()
    {
        if (Mode != GameMode.Survival)
        {
            return hand?.WinnerSeat ?? -1;
        }

        var left = Enumerable.Range(0, seats.Length).Where(seat => !outOfIt[seat]).ToList();

        if (left.Count == 1)
        {
            return left[0];
        }

        return Enumerable.Range(0, seats.Length)
            .Where(seat => seats[seat] is not null)
            .OrderBy(seat => totals[seat])
            .FirstOrDefault(-1);
    }

    /// <summary>Everything a new game starts from, which is nothing.</summary>
    private void ForgetTheGame()
    {
        Array.Clear(totals);
        Array.Clear(handsPlayed);
        Array.Clear(outOfIt);
        Array.Clear(lastHand);
        Array.Clear(readIt);
        handsSoFar = 0;
        showing = false;
    }
}
