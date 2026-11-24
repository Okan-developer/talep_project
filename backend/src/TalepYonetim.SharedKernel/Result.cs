namespace TalepYonetim.SharedKernel;

/// <summary>An expected business failure: a stable code plus a Turkish user-facing message.</summary>
public sealed record Error(string Code, string Message);

/// <summary>Outcome of an operation whose failure is expected (docs/conventions.md, Error handling).</summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public Error? Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    internal static Result<T> CreateSuccess(T value) => new(value);

    internal static Result<T> CreateFailure(Error error) => new(error);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.CreateFailure(error);
}
