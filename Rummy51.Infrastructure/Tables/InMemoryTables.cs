using Microsoft.Extensions.Options;
using Rummy51.Application.Authentication;
using Rummy51.Application.Common;
using Rummy51.Application.Tables;
using Rummy51.Domain.Entities;
using Rummy51.Domain.Game;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// Memory only, for the same reason the lobby roster is: a table that has not
/// dealt a card holds nothing worth restoring. A series carrying scores across
/// several hands will be a different question, and this is where it gets asked.
/// </summary>
public sealed class InMemoryTables(
    IOptions<TestingSettings> testing,
    IOptions<TurnSettings> turns,
    TimeProvider clock) : ITables
{
    private readonly bool markJokers = testing.Value.MarkJokers;

    // Two views of the same fact under one lock. The second answers the rule
    // that runs through everything here - a player sits at one table - without
    // walking every table to find out.
    private readonly Lock gate = new();
    private readonly Dictionary<Guid, OpenTable> tables = [];
    private readonly Dictionary<Guid, Guid> tableByPlayer = [];

    public Result<LobbyTable> Open(CreateTableRequest request, PlayerSummary player)
    {
        if (!TableOptions.Seats.Contains(request.Seats))
        {
            return Refused("TableSeatsInvalid", FailureKind.Validation);
        }

        if (!TableOptions.Jokers.Contains(request.Jokers))
        {
            return Refused("TableJokersInvalid", FailureKind.Validation);
        }

        // The form offers only what fits, but the form is not what decides.
        if (!TableOptions.Allows(request.Mode, request.Seats, request.Limit))
        {
            return Refused("TableLimitInvalid", FailureKind.Validation);
        }

        lock (gate)
        {
            if (tableByPlayer.ContainsKey(player.PlayerId))
            {
                return Refused("AlreadyAtATable", FailureKind.Conflict);
            }

            var table = new OpenTable(request, markJokers, turns.Value, clock);
            table.Seat(player);

            tables[table.Id] = table;
            tableByPlayer[player.PlayerId] = table.Id;

            return Result<LobbyTable>.Success(table.Describe());
        }
    }

    public Result<LobbyTable> Join(Guid tableId, PlayerSummary player)
    {
        lock (gate)
        {
            if (tableByPlayer.TryGetValue(player.PlayerId, out var seated))
            {
                // Arriving at the table they already sit at is how reopening
                // the page looks from here, and it is not a failure.
                return seated == tableId
                    ? Result<LobbyTable>.Success(tables[tableId].Describe())
                    : Refused("AlreadyAtATable", FailureKind.Conflict);
            }

            if (!tables.TryGetValue(tableId, out var table))
            {
                return Refused("TableNotFound", FailureKind.Validation);
            }

            if (table.Started)
            {
                return Refused("TableStarted", FailureKind.Conflict);
            }

            table.Seat(player);
            tableByPlayer[player.PlayerId] = tableId;

            return Result<LobbyTable>.Success(table.Describe());
        }
    }

    public bool Leave(Guid playerId)
    {
        lock (gate)
        {
            if (!tableByPlayer.Remove(playerId, out var tableId))
            {
                return false;
            }

            if (tables.TryGetValue(tableId, out var table))
            {
                table.Unseat(playerId);

                // Nobody left to come back to, so there is no table.
                if (table.IsEmpty)
                {
                    tables.Remove(tableId);
                }
            }

            return true;
        }
    }

    public IReadOnlyList<LobbyTable> All()
    {
        lock (gate)
        {
            // By id, which for version 7 is the order they were opened in.
            return [.. tables.Values.OrderBy(table => table.Id).Select(table => table.Describe())];
        }
    }

    public HandView? Hand(Guid tableId)
    {
        lock (gate)
        {
            return tables.TryGetValue(tableId, out var table) ? table.DescribeHand() : null;
        }
    }

    public Result<HandView> CutDeck(Guid tableId, Guid playerId, int position)
    {
        lock (gate)
        {
            if (!tables.TryGetValue(tableId, out var table))
            {
                return Result<HandView>.Fail(FailureKind.Validation, new Error("TableNotFound"));
            }

            if (table.CutterSeat is not { } cutter)
            {
                return Result<HandView>.Fail(FailureKind.Validation, new Error("HandNotStarted"));
            }

            if (table.SeatOf(playerId) != cutter)
            {
                return Result<HandView>.Fail(FailureKind.Validation, new Error("NotYoursToCut"));
            }

            if (!table.CutAt(position))
            {
                return Result<HandView>.Fail(FailureKind.Conflict, new Error("AlreadyCut"));
            }

            return Result<HandView>.Success(table.DescribeHand()!);
        }
    }

    public Result<HandView> Draw(Guid tableId, Guid playerId, DrawnFrom from) =>
        Move(tableId, playerId, (table, seat) => table.Draw(seat, from), "CannotDraw");

    public Result<HandView> Discard(Guid tableId, Guid playerId, int cardId) =>
        Move(tableId, playerId, (table, seat) => table.Discard(seat, cardId), "CannotDiscard");

    public Result<HandView> LayDown(Guid tableId, Guid playerId, IReadOnlyList<int> cardIds) =>
        Move(tableId, playerId, (table, seat) => table.LayDown(seat, cardIds), "CannotLayDown");

    public Result<HandView> Undo(Guid tableId, Guid playerId) =>
        Move(tableId, playerId, (table, seat) => table.Undo(seat), "NothingToUndo");

    /// <summary>
    /// A player's move, which always asks the same three questions before the
    /// move itself: is there a table, are they sitting at it, and did the hand
    /// allow what they asked for. The hand owns the rules; this owns who is
    /// asking. The refusal is one code per move rather than a reason, because
    /// a client only ever offers a move it believes is legal - one that comes
    /// back refused is a race or a bug, and neither has anything to tell the
    /// player.
    /// </summary>
    private Result<HandView> Move(
        Guid tableId,
        Guid playerId,
        Func<OpenTable, int, bool> move,
        string refusal)
    {
        lock (gate)
        {
            if (!tables.TryGetValue(tableId, out var table))
            {
                return Result<HandView>.Fail(FailureKind.Validation, new Error("TableNotFound"));
            }

            var seat = table.SeatOf(playerId);

            if (seat == -1)
            {
                return Result<HandView>.Fail(FailureKind.Validation, new Error("NotAtThisTable"));
            }

            if (!move(table, seat))
            {
                return Result<HandView>.Fail(FailureKind.Conflict, new Error(refusal));
            }

            return Result<HandView>.Success(table.DescribeHand()!);
        }
    }

    public Result<HandView> AddToMeld(Guid tableId, Guid playerId, AddRequest join) =>
        Move(
            tableId,
            playerId,
            (table, seat) => table.AddTo(seat, join.Meld, join.CardId, join.End),
            "CannotAddToMeld");

    public Result<HandView> SwapForJoker(Guid tableId, Guid playerId, SwapRequest swap) =>
        Move(
            tableId,
            playerId,
            (table, seat) => table.SwapForJoker(seat, swap.Meld, swap.At, swap.CardId),
            "CannotBuyThatJoker");

    public IReadOnlyList<HeldCardView> CardsOf(Guid tableId, Guid playerId)
    {
        lock (gate)
        {
            return tables.TryGetValue(tableId, out var table) ? table.CardsOf(playerId) : [];
        }
    }

    public IReadOnlyList<Guid> ActForTheOverdue()
    {
        lock (gate)
        {
            // Under the same lock as every other move, because the clock is
            // one more thing that changes a table and must not meet a move
            // halfway through.
            var acted = new List<Guid>();

            foreach (var table in tables.Values)
            {
                if (table.ActForWhoeverRanOut())
                {
                    acted.Add(table.Id);
                }
            }

            return acted;
        }
    }

    public LobbyTable? Find(Guid tableId)
    {
        lock (gate)
        {
            return tables.TryGetValue(tableId, out var table) ? table.Describe() : null;
        }
    }

    private static Result<LobbyTable> Refused(string code, FailureKind kind) =>
        Result<LobbyTable>.Fail(kind, new Error(code));
}
