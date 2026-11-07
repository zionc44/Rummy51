namespace Rummy51.Server.Hubs;

/// <summary>
/// The names the client listens on, in one place so the hub and the Angular
/// service cannot drift apart silently.
/// </summary>
public static class LobbyEvents
{
    public const string OnlinePlayersChanged = "OnlinePlayersChanged";

    public const string TablesChanged = "TablesChanged";

    /// <summary>Sent only to the connections watching that one table.</summary>
    public const string HandChanged = "HandChanged";

    /// <summary>Sent to a connection that a newer one has just replaced.</summary>
    public const string SignedInElsewhere = "SignedInElsewhere";
}
