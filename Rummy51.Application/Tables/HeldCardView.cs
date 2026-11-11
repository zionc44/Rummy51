using Rummy51.Domain.Game;

namespace Rummy51.Application.Tables;

/// <summary>
/// A place a card in a hand could join what is already on the table: which
/// meld, and which end of it.
/// </summary>
public sealed record FitView(int Meld, MeldEnd End);

/// <summary>
/// A joker lying on the table that this card is standing in for, and so
/// could buy out of it: which meld, and where in it the joker lies.
/// </summary>
public sealed record SwapView(int Meld, int At);

/// <summary>
/// One card in a player's own hand. The number belongs to the card and not
/// to the place it sits in: it is given when the card arrives and kept until
/// it leaves, so an arrangement made on the client survives every draw and
/// every discard that moves the cards around it.
/// </summary>
/// <param name="Fits">
/// Where this card could join a meld already lying on the table, which is
/// worked out here rather than on the screen. The screen has to offer the
/// place the moment a card is picked up, with nothing to wait for - and a
/// rule the screen works out for itself is a second copy of that rule.
/// </param>
/// <param name="Buys">
/// The jokers on the table this card is the card for, worked out here for
/// the same reason and sent the same way.
/// </param>
public sealed record HeldCardView(
    int Id,
    string Code,
    IReadOnlyList<FitView> Fits,
    IReadOnlyList<SwapView> Buys);
