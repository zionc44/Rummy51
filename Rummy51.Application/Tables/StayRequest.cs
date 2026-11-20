namespace Rummy51.Application.Tables;

/// <summary>Rule 9.3: another game at this table, or the chair given up.</summary>
public sealed record StayRequest
{
    public bool Another { get; init; }
}
