using Rummy51.Application.Authentication;
using Rummy51.Application.Common;
using Rummy51.Domain.Game;

namespace Rummy51.Application.Tables;

/// <summary>
/// Every table that is open, and who is sitting at each. One rule runs through
/// all of it: a player occupies at most one seat, so opening a table and
/// joining one are the same move and neither is possible while already seated.
/// </summary>
public interface ITables
{
    Result<LobbyTable> Open(CreateTableRequest request, PlayerSummary player);

    Result<LobbyTable> Join(Guid tableId, PlayerSummary player);

    /// <summary>
    /// Empties whichever seat the player holds. The table closes behind them
    /// when it was the last one taken.
    /// </summary>
    /// <returns>True when a seat was actually given up.</returns>
    bool Leave(Guid playerId);

    IReadOnlyList<LobbyTable> All();

    /// <summary>Null while it has not begun.</summary>
    HandView? Hand(Guid tableId);

    /// <summary>Rule 2.2.</summary>
    Result<HandView> CutDeck(Guid tableId, Guid playerId, int position);

    /// <summary>Rules 3.2, 7.1.</summary>
    Result<HandView> Draw(Guid tableId, Guid playerId, DrawnFrom from);

    /// <summary>Rule 3.2. The card is named by its number, the order being the player's own.</summary>
    Result<HandView> Discard(Guid tableId, Guid playerId, int cardId);

    /// <summary>Rules 4.1, 5.6.</summary>
    Result<HandView> LayDown(Guid tableId, Guid playerId, IReadOnlyList<int> cardIds);

    /// <summary>Rules 3.6, 7.4.</summary>
    Result<HandView> Undo(Guid tableId, Guid playerId);

    /// <summary>Rules 12.1, 12.2.</summary>
    Result<HandView> OfferARedeal(Guid tableId, Guid playerId);

    /// <summary>Rules 12.9 and 12.10.</summary>
    Result<HandView> ForceARedeal(Guid tableId, Guid playerId, bool atOnce);

    /// <summary>Rule 12.3.</summary>
    Result<HandView> Answer(Guid tableId, Guid playerId, bool agreed);

    /// <summary>Rule 12.4.</summary>
    Result<HandView> ReadTheResult(Guid tableId, Guid playerId);

    /// <summary>Rule 12.6.</summary>
    Result<HandView> Keep(Guid tableId, Guid playerId, IReadOnlyList<int> cardIds);

    /// <summary>Rules 5.7, 5.9, 5.10.</summary>
    Result<HandView> AddToMeld(Guid tableId, Guid playerId, AddRequest join);

    /// <summary>Rules 5.11, 5.12.</summary>
    Result<HandView> SwapForJoker(Guid tableId, Guid playerId, SwapRequest swap);

    /// <summary>
    /// Rule 11.1, which is why they are asked for separately rather than
    /// carried by <see cref="HandView"/>: they go to one connection and
    /// never to a group. Empty before the deal and for anybody not seated.
    /// </summary>
    IReadOnlyList<HeldCardView> CardsOf(Guid tableId, Guid playerId);

    /// <summary>
    /// Rule 12.4: the answers as this player may see them, which is nothing
    /// but the offerer's until they have answered themselves. Empty when
    /// there is no offer on the table.
    /// </summary>
    RedealView RedealAsSeenBy(Guid tableId, Guid playerId);

    /// <summary>
    /// Rule 3.6, and the tables that changed because of it. Called by the
    /// clock and by nobody else: the one move here that nobody asked for.
    /// </summary>
    IReadOnlyList<Guid> ActForTheOverdue();

    LobbyTable? Find(Guid tableId);
}
