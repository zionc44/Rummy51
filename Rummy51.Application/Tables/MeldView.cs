namespace Rummy51.Application.Tables;

/// <summary>Rule 11.3, and it has nowhere left to go.</summary>
public sealed record LaidCardView(int Id, string Code);

/// <summary>One run or set, and whose it was. Rules 5.7, 8.1, 11.3.</summary>
public sealed record MeldView(int Seat, IReadOnlyList<LaidCardView> Cards);
