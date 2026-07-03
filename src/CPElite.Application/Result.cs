namespace CPElite.Application;

public enum ErrorType
{
    Validation,
    Conflict,
    Unauthorized,
    Forbidden,
    NotFound
}

public sealed record AppError(ErrorType Type, string Code, string Message);

public sealed class Result<T>
{
    private Result(T? value, AppError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public AppError? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(ErrorType type, string code, string message) => new(default, new AppError(type, code, message));
}
