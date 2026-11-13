namespace Rummy51.Application.Account;

/// <summary>
/// Where an uploaded picture is served from. The controller that answers and
/// the summary that points at it have to agree, so the shape is written once.
/// </summary>
public static class AvatarRoute
{
    public const string Template = "api/players/{playerId:guid}/avatar";

    /// <summary>A new picture is a new address, so the answer caches for good.</summary>
    public static string For(Guid playerId, string tag) =>
        $"/api/players/{playerId}/avatar?v={tag}";
}
