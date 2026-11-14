namespace Rummy51.Application.Tables;

/// <summary>Rule 12.3: agreed, or not.</summary>
public sealed record AnswerRequest
{
    public bool Agreed { get; init; }
}
