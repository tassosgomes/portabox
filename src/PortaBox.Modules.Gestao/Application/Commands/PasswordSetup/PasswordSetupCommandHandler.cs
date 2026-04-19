using FluentValidation;
using Microsoft.Extensions.Logging;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Application.Abstractions.MagicLinks;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.Persistence;

namespace PortaBox.Modules.Gestao.Application.Commands.PasswordSetup;

public sealed class PasswordSetupCommandHandler(
    IValidator<PasswordSetupCommand> validator,
    IMagicLinkService magicLinkService,
    IIdentityPasswordService identityPasswordService,
    IApplicationDbSession dbSession,
    ILogger<PasswordSetupCommandHandler> logger) : ICommandHandler<PasswordSetupCommand, PasswordSetupResult>
{
    public async Task<Result<PasswordSetupResult>> HandleAsync(PasswordSetupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            LogFailure(null, "validation_failed");
            return Result<PasswordSetupResult>.Failure(PasswordSetupErrors.Generic);
        }

        await using var transaction = await dbSession.BeginTransactionAsync(cancellationToken);

        var consumeResult = await magicLinkService.ValidateAndConsumeAsync(
            command.RawToken,
            MagicLinkPurpose.PasswordSetup,
            cancellationToken);

        if (!consumeResult.IsSuccess || consumeResult.UserId is null)
        {
            LogFailure(consumeResult.UserId, ToReasonCode(consumeResult.FailureReason));
            return Result<PasswordSetupResult>.Failure(PasswordSetupErrors.Generic);
        }

        var passwordResult = await identityPasswordService.AddPasswordAsync(
            consumeResult.UserId.Value,
            command.Password,
            cancellationToken);

        if (!passwordResult.IsSuccess)
        {
            LogFailure(consumeResult.UserId, passwordResult.ErrorCode ?? "identity_failure");
            return Result<PasswordSetupResult>.Failure(PasswordSetupErrors.Generic);
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Password setup succeeded. {event} user_id={user_id}",
            "password-setup.succeeded",
            consumeResult.UserId);

        return Result<PasswordSetupResult>.Success(new PasswordSetupResult());
    }

    private void LogFailure(Guid? userId, string reasonCode)
    {
        logger.LogWarning(
            "Password setup failed. {event} user_id={user_id} reason_code={reason_code}",
            "password-setup.failed",
            userId,
            reasonCode);
    }

    private static string ToReasonCode(MagicLinkFailureReason failureReason)
    {
        return failureReason switch
        {
            MagicLinkFailureReason.Expired => "expired",
            MagicLinkFailureReason.AlreadyConsumed => "already_consumed",
            MagicLinkFailureReason.Invalidated => "invalidated",
            MagicLinkFailureReason.RateLimited => "rate_limited",
            _ => "not_found"
        };
    }
}
