using Rummy51.Domain.Game;

namespace Rummy51.Application.Tables;

/// <summary>Rule 5.9: which run or set, and which end of it.</summary>
public sealed record FitView(int Meld, MeldEnd End);

/// <summary>Rule 5.11: which run or set, and where in it the joker lies.</summary>
public sealed record SwapView(int Meld, int At);

/// <summary>
/// One card in a player's own hand, which rule 11.1 sends to nobody else.
/// The number belongs to the card and not to the place it sits in, so an
/// arrangement made on the client survives every draw and every discard that
/// moves the cards around it.
/// </summary>
/// <param name="Fits">
/// Worked out here and not on the screen, which has to answer the moment a
/// card is picked up - and a rule the screen works out is a second copy.
/// </param>
/// <param name="Buys">The same, for the jokers this card could buy.</param>
/// <param name="Owed">Rule 3.4, told only to the player who can act on it.</param>
public sealed record HeldCardView(
    int Id,
    string Code,
    IReadOnlyList<FitView> Fits,
    IReadOnlyList<SwapView> Buys,
    bool Owed);
