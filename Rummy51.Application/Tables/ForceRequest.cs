namespace Rummy51.Application.Tables;

/// <summary>Rule 12.10: ended there and then, or put to the table.</summary>
public sealed record ForceRequest
{
    public bool AtOnce { get; init; }
}
