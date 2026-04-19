using FluentValidation;
using Microsoft.Extensions.Logging;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;

public sealed class ActivateCondominioCommandHandler(
    IValidator<ActivateCondominioCommand> validator,
    ICondominioRepository condominioRepository,
    ITenantAuditRepository tenantAuditRepository,
    IApplicationDbSession dbSession,
    IGestaoMetrics metrics,
    ILogger<ActivateCondominioCommandHandler> logger,
    TimeProvider timeProvider) : ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult>
{
    public async Task<Result<ActivateCondominioResult>> HandleAsync(ActivateCondominioCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var condominio = await condominioRepository.GetByIdAsync(command.CondominioId, cancellationToken);
        if (condominio is null)
        {
            return Result<ActivateCondominioResult>.Failure(ActivateCondominioErrors.NotFound);
        }

        if (!condominio.TryActivate(command.PerformedByUserId, timeProvider, out _))
        {
            return Result<ActivateCondominioResult>.Failure(ActivateCondominioErrors.AlreadyActive);
        }

        await condominioRepository.UpdateAsync(condominio, cancellationToken);
        await tenantAuditRepository.AddAsync(
            TenantAuditEntry.Create(
                condominio.Id,
                TenantAuditEventKind.Activated,
                command.PerformedByUserId,
                condominio.ActivatedAt ?? timeProvider.GetUtcNow(),
                command.Note),
            cancellationToken);

        await dbSession.SaveChangesAsync(cancellationToken);
        metrics.IncrementCondominioActivated();

        logger.LogInformation(
            "Condominio activated. {event} condominio_id={condominio_id} activated_by={activated_by} tenant_id={tenant_id} user_id={user_id} note={note}",
            "condominio.activated",
            condominio.Id,
            command.PerformedByUserId,
            condominio.Id,
            command.PerformedByUserId,
            string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim());

        return Result<ActivateCondominioResult>.Success(new ActivateCondominioResult(condominio.Id));
    }
}
