namespace Rummy51.Domain.Entities;

/// <summary>Rules 12.3 and 12.4: what one player said to one redeal.</summary>
public class RedealAnswer
{
    public Guid HandRedealId { get; set; }

    public Guid PlayerId { get; set; }

    public RedealReply Reply { get; set; }
}
