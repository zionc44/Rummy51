namespace Rummy51.Domain.Entities;

/// <summary>
/// What one seat said to a redeal. Rule 12.4 makes silence agreement and
/// rule 12.10 forces without asking at all; both are kept apart from a
/// plain yes, because they are the same to the game and not to anybody
/// reading them back, where silence is somebody who had walked away.
/// </summary>
public enum RedealReply
{
    Offered = 0,
    Agreed = 1,
    Refused = 2,
    Silent = 3,
    NotAsked = 4,
}
