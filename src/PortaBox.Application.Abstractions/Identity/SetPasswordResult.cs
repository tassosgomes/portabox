namespace PortaBox.Application.Abstractions.Identity;

public sealed record SetPasswordResult(
    bool IsSuccess,
    string? ErrorCode)
{
    public static SetPasswordResult Success() => new(true, null);

    public static SetPasswordResult Failure(string errorCode) => new(false, errorCode);
}
