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

    /// <summary>The hand at that table, or null while it has not begun.</summary>
    HandView? Hand(Guid tableId);

    /// <summary>
    /// The cutter chose where to cut. The card there becomes the finishing
    /// card, and the one after it is the first that will be dealt.
    /// </summary>
    Result<HandView> CutDeck(Guid tableId, Guid playerId, int position);

    /// <summary>
    /// The first half of a turn: a card from the stock, or the one the player
    /// before threw away.
    /// </summary>
    Result<HandView> Draw(Guid tableId, Guid playerId, DrawnFrom from);

    /// <summary>
    /// The second half, which ends the turn and passes it on. The card is
    /// named by its code, since the hand's order is the player's own.
    /// </summary>
    Result<HandView> Discard(Guid tableId, Guid playerId, int cardId);

    /// <summary>
    /// One player's own cards. They go to that player's own connection and
    /// never to a group, which is why they are asked for separately rather
    /// than carried by <see cref="HandView"/>. Empty before the deal, and
    /// for anybody not seated at that table.
    /// </summary>
    IReadOnlyList<HeldCardView> CardsOf(Guid tableId, Guid playerId);

    /// <summary>
    /// Acts for every player whose time has run out, and names the tables
    /// that changed because of it. Called by the clock and by nobody else:
    /// it is the one move here that nobody asked for.
    /// </summary>
    IReadOnlyList<Guid> ActForTheOverdue();

    LobbyTable? Find(Guid tableId);
}
