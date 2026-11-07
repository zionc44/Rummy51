using Rummy51.Application.Authentication;
using Rummy51.Application.Common;
using Rummy51.Application.Tables;
using Rummy51.Domain.Entities;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// Memory only, for the same reason the lobby roster is: a table that has not
/// dealt a card holds nothing worth restoring. A series carrying scores across
/// several hands will be a different question, and this is where it gets asked.
/// </summary>
public sealed class InMemoryTables : ITables
{
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

            var table = new OpenTable(request);
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
