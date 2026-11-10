namespace Rummy51.Application.Tables;

/// <summary>
/// One card in a player's own hand. The number belongs to the card and not
/// to the place it sits in: it is given when the card arrives and kept until
/// it leaves, so an arrangement made on the client survives every draw and
/// every discard that moves the cards around it.
/// </summary>
public sealed record HeldCardView(int Id, string Code);
