namespace Rummy51.Application.Tables;

/// <summary>
/// A card lying on the table. Anybody may read it and nobody is holding it,
/// which is the whole difference between this and a card in a hand: a card
/// on the table has nowhere left to go.
/// </summary>
public sealed record LaidCardView(int Id, string Code);

/// <summary>
/// One run or set lying on the table, and whose it was. Every card of it is
/// named: once it is down it belongs to the table, and there is nothing in
/// it left to keep from anybody.
/// </summary>
public sealed record MeldView(int Seat, IReadOnlyList<LaidCardView> Cards);
