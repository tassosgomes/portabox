namespace PortaBox.Application.Abstractions.Identity;

public sealed record CreateSindicoUserResult(
    bool IsSuccess,
    IdentityUserDescriptor? User,
    string? Error)
{
    public static CreateSindicoUserResult Success(IdentityUserDescriptor user) => new(true, user, null);

    public static CreateSindicoUserResult Failure(string error) => new(false, null, error);
}
