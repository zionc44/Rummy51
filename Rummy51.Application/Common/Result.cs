namespace Rummy51.Application.Common;

/// <summary>
/// Why an operation failed, so a caller can translate it into its own
/// vocabulary (an HTTP status code, say) without reading error strings.
/// </summary>
public enum FailureKind
{
    Validation,
    Conflict
}

public sealed record Error(string Code, string Description);

/// <summary>
/// The outcome of an operation whose failures are expected rather than
/// exceptional — a taken email address is an answer, not a fault.
/// </summary>
public sealed class Result<T>
{
    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Errors = [];
    }

    private Result(FailureKind failure, IReadOnlyList<Error> errors)
    {
        Failure = failure;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public FailureKind? Failure { get; }
    public IReadOnlyList<Error> Errors { get; }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Fail(FailureKind failure, params Error[] errors) => new(failure, errors);
}
