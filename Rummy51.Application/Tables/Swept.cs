namespace Rummy51.Application.Tables;

/// <summary>
/// What one pass of the clock changed. A table that closed is not a table
/// that moved: nobody is left in its group to be told about it, so the only
/// way anybody hears is the list going out to everybody. A chair given up
/// under rule 10.8 is the same kind of news for a different reason - the
/// people who need it are the ones not at that table.
/// </summary>
public sealed record Swept(IReadOnlyList<Guid> Acted, bool ListChanged);
