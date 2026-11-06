namespace Rummy51.Domain.Entities;

/// <summary>
/// A picture the player uploaded. It is kept out of the player row because
/// Identity reads that row on every authenticated request, and this is the
/// only part of a player measured in kilobytes.
/// </summary>
public class PlayerAvatar
{
    public Guid PlayerId { get; set; }

    /// <summary>Always what the server encoded, never what arrived.</summary>
    public required byte[] Image { get; set; }

    public required string ContentType { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
