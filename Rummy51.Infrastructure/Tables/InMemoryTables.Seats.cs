using Rummy51.Application.Authentication;
using Rummy51.Application.Common;
using Rummy51.Domain.Entities;
using Rummy51.Application.Tables;

namespace Rummy51.Infrastructure.Tables;

/// <summary>
/// Who is sitting where, and which of those chairs they are actually in.
/// Chapter 10 makes those one question: a seat is held or given up by
/// whether anybody is watching it, so sitting down, walking out, dropping
/// and coming back all arrive here and nowhere else.
/// </summary>
public sealed partial class InMemoryTables
{
    /// <summary>
    /// Rules 10.5 and 10.3.
    /// </summary>
    private readonly Dictionary<Guid, HashSet<Guid>> tablesByPlayer = [];

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
            if (WaitingSomewhere(player.PlayerId))
            {
                return Refused("AlreadyAtATable", FailureKind.Conflict);
            }

            var table = new OpenTable(request, testing, turns.Value, clock);
            table.Seat(player);

            tables[table.Id] = table;
            Sat(player.PlayerId, table.Id);
            AwayFromAllBut(player.PlayerId, table.Id);

            return Result<LobbyTable>.Success(table.Describe());
        }
    }

    public Result<LobbyTable> Join(Guid tableId, PlayerSummary player)
    {
        lock (gate)
        {
            // Arriving at a table they already sit at is how reopening the
            // page looks from here, and it is not a failure.
            if (Seated(player.PlayerId).Contains(tableId))
            {
                AwayFromAllBut(player.PlayerId, tableId);

                return Result<LobbyTable>.Success(tables[tableId].Describe());
            }

            if (WaitingSomewhere(player.PlayerId))
            {
                return Refused("AlreadyAtATable", FailureKind.Conflict);
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
            Sat(player.PlayerId, tableId);
            AwayFromAllBut(player.PlayerId, tableId);

            return Result<LobbyTable>.Success(table.Describe());
        }
    }

    /// <summary>
    /// Rules 10.2 and 10.3.
    /// </summary>
    public bool WentAway(Guid playerId, Guid tableId)
    {
        lock (gate)
        {
            return Went(playerId, tableId);
        }
    }

    public bool WentAwayFromEverything(Guid playerId)
    {
        lock (gate)
        {
            var left = Seated(playerId).ToList();

            foreach (var tableId in left)
            {
                Went(playerId, tableId);
            }

            return left.Count > 0;
        }
    }

    /// <summary>
    /// Where they are now, which makes them absent everywhere else. One
    /// connection to a player means one table being watched at a time, so
    /// this is the whole of what decides who is present.
    /// </summary>
    public bool Watching(Guid playerId, Guid tableId)
    {
        lock (gate)
        {
            AwayFromAllBut(playerId, tableId);

            return tables.TryGetValue(tableId, out var table) && table.CameBack(playerId);
        }
    }

    /// <summary>Rule 10.2: being at one table is not being at the others.</summary>
    private void AwayFromAllBut(Guid playerId, Guid tableId)
    {
        foreach (var other in Seated(playerId).Where(at => at != tableId).ToList())
        {
            Went(playerId, other);
        }
    }

    private bool Went(Guid playerId, Guid tableId)
    {
        if (!Seated(playerId).Contains(tableId) || !tables.TryGetValue(tableId, out var table))
        {
            Stood(playerId, tableId);

            return false;
        }

        if (!table.WentAway(playerId))
        {
            return false;
        }

        // A seat held is a player still seated, so the lobby goes on saying
        // where they are - which is also what offers them the way back.
        if (!table.Started)
        {
            Stood(playerId, tableId);
        }

        // Rule 10.7, and it is this move that can have made it true.
        if (table.Deserted)
        {
            Forget(table);
        }

        return true;
    }

    private bool WaitingSomewhere(Guid playerId) =>
        Seated(playerId).Any(at => tables.TryGetValue(at, out var table) && !table.Started);

    private IReadOnlyCollection<Guid> Seated(Guid playerId) =>
        tablesByPlayer.TryGetValue(playerId, out var at) ? at : [];

    private void Sat(Guid playerId, Guid tableId)
    {
        if (!tablesByPlayer.TryGetValue(playerId, out var at))
        {
            tablesByPlayer[playerId] = at = [];
        }

        at.Add(tableId);
    }

    private void Stood(Guid playerId, Guid tableId)
    {
        if (tablesByPlayer.TryGetValue(playerId, out var at) && at.Remove(tableId) && at.Count == 0)
        {
            tablesByPlayer.Remove(playerId);
        }
    }

    /// <summary>A table nobody is at, or nobody is coming back to.</summary>
    private void Forget(OpenTable table)
    {
        tables.Remove(table.Id);

        foreach (var playerId in tablesByPlayer.Keys.ToList())
        {
            Stood(playerId, table.Id);
        }
    }
}
