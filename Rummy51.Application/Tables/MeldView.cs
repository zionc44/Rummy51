namespace Rummy51.Application.Tables;

/// <summary>
/// One run or set lying on the table, and whose it was. Every card of it is
/// named: once it is down it belongs to the table, and there is nothing in
/// it left to keep from anybody.
/// </summary>
public sealed record MeldView(int Seat, IReadOnlyList<HeldCardView> Cards);
