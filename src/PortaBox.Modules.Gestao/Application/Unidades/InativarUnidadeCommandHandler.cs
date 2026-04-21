using FluentValidation;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Modules.Gestao.Application.Unidades;

public sealed class InativarUnidadeCommandHandler(
    IValidator<InativarUnidadeCommand> validator,
    IUnidadeRepository unidadeRepository,
    IAuditService auditService,
    TimeProvider timeProvider) : ICommandHandler<InativarUnidadeCommand, UnidadeDto>
{
    public async Task<Result<UnidadeDto>> HandleAsync(InativarUnidadeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var unidade = await unidadeRepository.GetByIdIncludingInactiveAsync(command.UnidadeId, cancellationToken);
        if (unidade is null || unidade.BlocoId != command.BlocoId)
        {
            return Result<UnidadeDto>.Failure("Unidade nao encontrada");
        }

        var inativarResult = unidade.Inativar(command.PerformedByUserId, timeProvider.GetUtcNow().UtcDateTime);
        if (!inativarResult.IsSuccess)
        {
            return Result<UnidadeDto>.Failure(inativarResult.Error ?? "Nao foi possivel inativar a unidade.");
        }

        await auditService.RecordStructuralAsync(
            TenantAuditEventKind.UnidadeInativada,
            unidade.TenantId,
            command.PerformedByUserId,
            StructuralAuditMetadata.ForUnidadeInativada(unidade.Id, unidade.BlocoId, unidade.Andar, unidade.Numero),
            $"Unidade '{unidade.Numero}' inativada",
            cancellationToken);
        await unidadeRepository.SaveAsync(cancellationToken);

        return Result<UnidadeDto>.Success(ToDto(unidade));
    }

    private static UnidadeDto ToDto(Unidade unidade)
    {
        return new UnidadeDto(unidade.Id, unidade.BlocoId, unidade.Andar, unidade.Numero, unidade.Ativo, unidade.InativadoEm);
    }
}
