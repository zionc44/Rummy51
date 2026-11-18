namespace Rummy51.Application.Games;

/// <summary>
/// Writes down what a table produced. It is asked outside whatever lock a
/// table is kept under, because a database that is slow must not be a table
/// that has stopped - and it may fail without the game failing with it: a
/// result that was not written is worth less than a game that stopped.
/// </summary>
public interface IGameRecords
{
    Task WriteAsync(IReadOnlyList<GameRecord> records, CancellationToken stopping);
}
