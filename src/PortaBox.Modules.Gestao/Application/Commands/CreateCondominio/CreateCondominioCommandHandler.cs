using FluentValidation;
using Microsoft.Extensions.Logging;
using PortaBox.Application.Abstractions.Identity;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Persistence;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.Observability;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Events;

namespace PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;

public sealed class CreateCondominioCommandHandler(
    IValidator<CreateCondominioCommand> validator,
    ICondominioRepository condominioRepository,
    ISindicoRepository sindicoRepository,
    IOptInRecordRepository optInRecordRepository,
    ITenantAuditRepository tenantAuditRepository,
    IIdentityUserProvisioningService identityUserProvisioningService,
    IApplicationDbSession dbSession,
    IGestaoMetrics metrics,
    ILogger<CreateCondominioCommandHandler> logger,
    TimeProvider timeProvider) : ICommandHandler<CreateCondominioCommand, CreateCondominioResult>
{
    public async Task<Result<CreateCondominioResult>> HandleAsync(CreateCondominioCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        if (await condominioRepository.ExistsByCnpjAsync(command.Cnpj, cancellationToken))
        {
            return Result<CreateCondominioResult>.Failure(CreateCondominioErrors.CnpjAlreadyExists);
        }

        var condominioId = Guid.NewGuid();
        var userCreationResult = await identityUserProvisioningService.CreateSindicoUserAsync(
            new CreateSindicoUserCommand(
                command.SindicoEmail,
                command.SindicoNomeCompleto,
                condominioId),
            cancellationToken);

        if (!userCreationResult.IsSuccess || userCreationResult.User is null)
        {
            return Result<CreateCondominioResult>.Failure(userCreationResult.Error ?? CreateCondominioErrors.SindicoUserCreationFailed);
        }

        var condominio = Condominio.Create(
            condominioId,
            command.NomeFantasia,
            command.Cnpj,
            command.CreatedByUserId,
            timeProvider,
            command.EnderecoLogradouro,
            command.EnderecoNumero,
            command.EnderecoComplemento,
            command.EnderecoBairro,
            command.EnderecoCidade,
            command.EnderecoUf,
            command.EnderecoCep,
            command.AdministradoraNome);

        var sindico = Sindico.Create(
            Guid.NewGuid(),
            condominioId,
            userCreationResult.User.UserId,
            command.SindicoNomeCompleto,
            command.SindicoCelularE164,
            timeProvider);

        var optInRecord = OptInRecord.Create(
            Guid.NewGuid(),
            condominioId,
            command.DataAssembleia,
            command.QuorumDescricao,
            command.SignatarioNome,
            command.SignatarioCpf,
            command.DataTermo,
            command.CreatedByUserId,
            timeProvider);

        var auditEntry = TenantAuditEntry.Create(
            condominioId,
            TenantAuditEventKind.Created,
            command.CreatedByUserId,
            timeProvider.GetUtcNow());

        condominio.RaiseCadastrado(
            userCreationResult.User.UserId,
            command.SindicoNomeCompleto,
            command.SindicoEmail,
            timeProvider.GetUtcNow());

        await condominioRepository.AddAsync(condominio, cancellationToken);
        await sindicoRepository.AddAsync(sindico, cancellationToken);
        await optInRecordRepository.AddAsync(optInRecord, cancellationToken);
        await tenantAuditRepository.AddAsync(auditEntry, cancellationToken);

        await dbSession.SaveChangesAsync(cancellationToken);
        metrics.IncrementCondominioCreated();

        logger.LogInformation(
            "Condominio created. {event} condominio_id={condominio_id} cnpj_suffix={cnpj_suffix} tenant_id={tenant_id} user_id={user_id}",
            "condominio.created",
            condominioId,
            command.Cnpj.Where(char.IsDigit).TakeLast(4).Aggregate(string.Empty, (current, character) => current + character),
            condominioId,
            command.CreatedByUserId);

        return Result<CreateCondominioResult>.Success(new CreateCondominioResult(condominioId, userCreationResult.User.UserId));
    }
}
