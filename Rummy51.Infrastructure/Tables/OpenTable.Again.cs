using Rummy51.Domain.Entities;
using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// Rules 9.3 and 10.8: what a table does once its game is over. It asks
/// who wants another and the answers decide the rest - everybody staying
/// deals again, anybody going leaves a chair for the lobby to fill.
/// </summary>
internal sealed partial class OpenTable
{
    /// <summary>Null while nobody is being asked; a seat is null until it has answered.</summary>
    private bool?[]? staying;

    /// <summary>Rule 2.1, kept because rule 9.3 moves the deal on from it.</summary>
    private int dealerWas = -1;

    /// <summary>Whoever rule 10.8 has finished with, for the lobby to forget.</summary>
    private readonly List<Guid> letGo = [];

    /// <summary>
    /// The row this table shows in the lobby is out of date. Letting
    /// somebody go is not the whole of it: a table whose game is over stops
    /// being one anybody is told about, and nothing else would ever say so,
    /// because the people who need to hear it are the ones not sitting here.
    /// </summary>
    private bool rowChanged;

    /// <summary>Rule 9.3.</summary>
    private void AskWhoIsStaying()
    {
        // Only ever reached once the game itself is over: rule 9.4 deals
        // another hand instead while anybody is still in it.
        if (staying is not null || hand?.Phase != HandPhase.Finished)
        {
            return;
        }

        staying = new bool?[seats.Length];
        Wait(turns.StaySeconds);
    }

    /// <summary>Rule 9.3, answered.</summary>
    public bool Stay(int seat, bool another)
    {
        if (staying is null || seat < 0 || seat >= seats.Length || staying[seat] is not null)
        {
            return false;
        }

        staying[seat] = another;

        // Rule 10.8: the answer is the giving up, and waiting for the rest
        // of the table to speak would leave everybody looking at somebody
        // sitting in a chair they have already left.
        if (!another)
        {
            GiveUp(seat);
        }

        if (EverybodyStillHereHasSaid())
        {
            Settle();
        }

        return true;
    }

    /// <summary>
    /// Rule 9.3 is put to players and not to chairs. Rule 9.6 empties one in
    /// the middle of a game, and an empty one can never answer: a table that
    /// waited for it would sit out the whole of rule 10.8's clock with
    /// everybody who is there already agreed.
    ///
    /// Somebody seated but not looking is waited for all the same. Not
    /// answering costs them the chair, and 10.8 gives them the time.
    /// </summary>
    private bool EverybodyStillHereHasSaid() =>
        Enumerable.Range(0, seats.Length)
            .All(seat => seats[seat] is null || staying![seat] is not null);

    /// <summary>Rule 10.8: not answering is giving the seat up.</summary>
    private bool NobodyElseIsStaying()
    {
        if (staying is null)
        {
            return false;
        }

        Settle();

        return true;
    }

    private void Settle()
    {
        for (var seat = 0; seat < seats.Length; seat++)
        {
            if (staying![seat] != true)
            {
                GiveUp(seat);
            }
        }

        staying = null;
        hand = null;
        Started = seats.All(seat => seat is not null);
        rowChanged = true;

        if (Started)
        {
            // Rule 9.3: the same people in the same places, so rule 2.1
            // moves the deal on exactly as it does between hands.
            BeginAGame((dealerWas + 1) % seats.Length);

            return;
        }

        // Nothing is being waited for until somebody takes the chair, and a
        // wait left standing is asked again on every tick for ever.
        countdown.Stop();
    }

    /// <summary>
    /// The chair is empty from this moment and the lobby is told at the next
    /// sweep. <see cref="Started"/> is left alone until the question is over,
    /// so that nobody takes the chair while the rest are still answering.
    /// </summary>
    private void GiveUp(int seat)
    {
        if (seats[seat] is not { } going)
        {
            return;
        }

        letGo.Add(going.PlayerId);
        seats[seat] = null;
        gone[seat] = null;
        here[seat] = false;
        rowChanged = true;
    }

    /// <summary>Whether the lobby's list has to be sent out again.</summary>
    public bool TakeRowChange()
    {
        var changed = rowChanged;

        rowChanged = false;

        return changed;
    }

    /// <summary>
    /// Rule 2.1, among whoever rule 9.4 has left in it. The testing aids
    /// are let go of here rather than kept for the table's life: they are
    /// about a first deal, and every hand has one. A redeal is not one,
    /// which is what the flag they turn off goes on meaning.
    /// </summary>
    private void Deal(int dealerSeat)
    {
        dealerWas = dealerSeat;
        hand = new Hand(seats.Length, Jokers, dealerSeat, Random.Shared.Next(), StillIn);
        handWritten = false;
        dealtOnce = false;

        Wait(turns.CutSeconds);
    }

    /// <summary>
    /// Rules 9.1 and 10.4: a game of its own, from nothing. What ran across
    /// the last one is forgotten before the first hand is dealt, or the new
    /// game would begin with somebody already out of it.
    /// </summary>
    private void BeginAGame(int dealerSeat)
    {
        ForgetTheGame();
        NoteTheGameBegan();
        Deal(dealerSeat);
    }
}
