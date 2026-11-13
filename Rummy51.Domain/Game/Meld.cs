namespace Rummy51.Domain.Game;

/// <summary>Cards on the table and whose they were. Rules 5.7 and 8.1.</summary>
public sealed record Meld(int Seat, IReadOnlyList<HeldCard> Cards);
