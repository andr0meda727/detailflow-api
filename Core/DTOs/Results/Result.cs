namespace Core.DTOs.Results;

public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }
    public bool Success => Error is null;
    public string? ErrorCode => Error?.Code;
    public string? ErrorMessage => Error?.Message;

    public static Result Ok() => new(null);
    public static Result Fail(Error error) => new(error);

    public static implicit operator Result(Error error) => Fail(error);
}

public sealed class Result<T> : Result
{
    private Result(T? value, Error? error) : base(error) => Value = value;

    public T? Value { get; }

    public static Result<T> Ok(T value) => new(value, null);
    public new static Result<T> Fail(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);
}
