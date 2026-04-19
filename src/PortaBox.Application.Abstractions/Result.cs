namespace PortaBox.Application.Abstractions;

public sealed class Result<TResult>
{
    private Result(bool isSuccess, TResult? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public TResult? Value { get; }

    public string? Error { get; }

    public static Result<TResult> Success(TResult value) => new(true, value, null);

    public static Result<TResult> Failure(string error) => new(false, default, error);
}
