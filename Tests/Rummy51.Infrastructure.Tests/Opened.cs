using Microsoft.Extensions.Options;
using Rummy51.Application.Authentication;
using Rummy51.Application.Common;

namespace Rummy51.Infrastructure.Tests;

/// <summary>
/// A table with people at it and a clock that moves only when a test moves
/// it. Everything here goes through <see cref="InMemoryTables"/> rather than
/// reaching for the table behind it, because who is asking is half of what
/// this project decides and a test that stepped over that would be holding
/// the other half to nothing.
///
/// Nothing about a deal is predictable: the dealer is drawn at random and so
/// is the seed. So nothing here names a seat it was not told, and a test
/// that wants the player in turn asks the hand who that is.
/// </summary>
internal sealed class Opened
{
    private readonly List<PlayerSummary> players = [];

    private Opened(InMemoryTables tables, FakeTimeProvider clock, Guid id, TurnSettings turns)
    {
        Tables = tables;
        Clock = clock;
        Id = id;
        Turns = turns;
    }

    public InMemoryTables Tables { get; }

    public FakeTimeProvider Clock { get; }

    public Guid Id { get; }

    public TurnSettings Turns { get; }

    /// <summary>In seat order, which is the order they sat down in.</summary>
    public IReadOnlyList<PlayerSummary> Players => players;

    public HandView Hand => Tables.Hand(Id)!;

    public LobbyTable Row => Tables.Find(Id)!;

    public Guid Who(int seat) => players[seat].PlayerId;

    /// <summary>No tables at all, for the questions asked before there are any.</summary>
    public static InMemoryTables None() =>
        new(
            Options.Create(new TestingSettings()),
            Options.Create(new TurnSettings()),
            new FakeTimeProvider());

    /// <summary>A table with one player at it and the rest of its chairs free.</summary>
    public static Opened For(
        int seats,
        GameMode mode = GameMode.Single,
        int? limit = null,
        int jokers = 2,
        TurnSettings? turns = null,
        TestingSettings? testing = null)
    {
        var clock = new FakeTimeProvider();
        var settings = turns ?? new TurnSettings();
        var tables = new InMemoryTables(
            Options.Create(testing ?? new TestingSettings()),
            Options.Create(settings),
            clock);

        var opener = Somebody();
        var opened = tables.Open(
            new CreateTableRequest { Seats = seats, Jokers = jokers, Mode = mode, Limit = limit },
            opener);

        var table = new Opened(tables, clock, opened.Value!.TableId, settings);

        table.players.Add(opener);

        return table;
    }

    /// <summary>And filled, so a hand is waiting to be cut.</summary>
    public static Opened Filled(
        int seats = 3,
        GameMode mode = GameMode.Single,
        int? limit = null,
        int jokers = 2,
        TurnSettings? turns = null,
        TestingSettings? testing = null)
    {
        var table = For(seats, mode, limit, jokers, turns, testing);

        while (!table.Row.Started)
        {
            table.Join();
        }

        return table;
    }

    /// <summary>
    /// Filled, dealt, and dealt on purpose: the player after the dealer is
    /// holding nothing but melds. A hand that can be gone out with is one
    /// deal in some enormous number, and a suite that waited for one would
    /// be a suite nobody runs.
    /// </summary>
    public static Opened Arranged(
        int seats = 2,
        GameMode mode = GameMode.Single,
        int? limit = null,
        TurnSettings? turns = null) =>
        Filled(seats, mode, limit, turns: turns, testing: new TestingSettings { ArrangedHand = true })
            .Dealt();

    /// <summary>
    /// Played by the clock until the stock is gone, which nobody reaches by
    /// hand: sixty-odd turns, and here a few hundredths of a second.
    /// </summary>
    public Opened PlayedDry()
    {
        while (Hand.Phase == HandPhase.Playing)
        {
            Tick(Turns.TurnSeconds);
        }

        return this;
    }

    /// <summary>
    /// The arranged hand played out to rule 6.1, by whoever is holding it.
    /// Rules 6.4 and 6.5 make it two turns and not one: nothing goes down
    /// in the first round, so one card is thrown and the rest laid next
    /// time round.
    ///
    /// All of it goes down at once, which is rule 6.6's one strike, so
    /// every hand that ends this way is doubled. Nothing here asserts a
    /// figure for that reason - what a hand is worth is the domain's.
    /// </summary>
    public Opened GoOut()
    {
        var seat = Hand.TurnSeat;
        var player = Who(seat);
        var cards = Tables.CardsOf(Id, player).Select(card => card.Id).ToList();

        Tables.Discard(Id, player, cards[^1]);

        while (Hand.TurnSeat != seat)
        {
            Tick(Turns.TurnSeconds);
        }

        Tables.Draw(Id, player, DrawnFrom.Stock);

        var drawn = Tables.CardsOf(Id, player).Select(card => card.Id).Except(cards).Single();

        Tables.LayDown(Id, player, [.. cards[0..4]]);
        Tables.LayDown(Id, player, [.. cards[4..8]]);
        Tables.LayDown(Id, player, [.. cards[8..11]]);
        Tables.LayDown(Id, player, [.. cards[11..14]]);
        Tables.Discard(Id, player, drawn);

        return this;
    }

    /// <summary>One more newcomer into the first chair that is free.</summary>
    public PlayerSummary Join()
    {
        var player = Somebody();

        Tables.Join(Id, player);
        players.Add(player);

        return player;
    }

    /// <summary>Rule 2.2, by whoever it falls to, and the deal that follows.</summary>
    public Opened Cut(int position = 0)
    {
        Tables.CutDeck(Id, Who(Hand.CutterSeat), position);

        return this;
    }

    /// <summary>Cut, and the cards seen out, so the first turn is somebody's.</summary>
    public Opened Dealt(int position = 0)
    {
        Cut(position);
        Tick(Turns.DealSeconds);

        return this;
    }

    /// <summary>The clock forward, and the sweep that is the only thing watching it.</summary>
    public Swept Tick(int seconds)
    {
        Clock.Advance(TimeSpan.FromSeconds(seconds));

        return Tables.ActForTheOverdue();
    }

    /// <summary>Long enough that whatever was being waited for has been acted on.</summary>
    public Swept TickOut() => Tick(Turns.KeepSeconds + 1);

    /// <summary>A whole turn by whoever it belongs to: a card in and a card out.</summary>
    public Opened PlayOut()
    {
        var seat = Hand.TurnSeat;
        var playerId = Who(seat);

        Tables.Draw(Id, playerId, DrawnFrom.Stock);
        Tables.Discard(Id, playerId, Tables.CardsOf(Id, playerId)[0].Id);

        return this;
    }

    /// <summary>Rules 10.2 and 10.3: the page closed on this table.</summary>
    public Opened Away(int seat)
    {
        Tables.WentAway(Who(seat), Id);

        return this;
    }

    /// <summary>Rule 10.3: the page open on it again.</summary>
    public Opened Back(int seat)
    {
        Tables.Watching(Who(seat), Id);

        return this;
    }

    /// <summary>A player nobody has seen before.</summary>
    public static PlayerSummary Somebody()
    {
        var id = Guid.NewGuid();

        return new PlayerSummary(
            id,
            $"{id:N}@example.test",
            $"Player {id:N}"[..12],
            null,
            null,
            null,
            AvatarSource.Provider,
            true);
    }
}
