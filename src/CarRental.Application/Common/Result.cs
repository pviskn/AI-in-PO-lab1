namespace CarRental.Application.Common;

public sealed class Result<T>
{
    public T? Value { get; }

    public string? Error { get; }

    public string? ErrorCode { get; }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Result(string error, string? errorCode = null)
    {
        Error = error;
        ErrorCode = errorCode;
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(string error, string? errorCode = null) => new(error, errorCode);
}
