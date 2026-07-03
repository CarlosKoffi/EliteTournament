namespace CPElite.Web.Services;

public sealed record ApiResult<T>(bool Success, T? Value, string? Error)
{
    public static ApiResult<T> Ok(T value) => new(true, value, null);

    public static ApiResult<T> Fail(string error) => new(false, default, error);
}
