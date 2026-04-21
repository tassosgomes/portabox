using FluentValidation;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed class InativarBlocoCommandHandler(
    IValidator<InativarBlocoCommand> validator,
    IBlocoRepository blocoRepository,
    IAuditService auditService,
    TimeProvider timeProvider) : ICommandHandler<InativarBlocoCommand, BlocoDto>
{
    public async Task<Result<BlocoDto>> HandleAsync(InativarBlocoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var bloco = await blocoRepository.GetByIdIncludingInactiveAsync(command.BlocoId, cancellationToken);
        if (bloco is null || bloco.CondominioId != command.CondominioId)
        {
            return Result<BlocoDto>.Failure("Bloco nao encontrado");
        }

        var inativarResult = bloco.Inativar(command.PerformedByUserId, timeProvider.GetUtcNow().UtcDateTime);
        if (!inativarResult.IsSuccess)
        {
            return Result<BlocoDto>.Failure(inativarResult.Error ?? "Nao foi possivel inativar o bloco.");
        }

        await auditService.RecordStructuralAsync(
            TenantAuditEventKind.BlocoInativado,
            bloco.TenantId,
            command.PerformedByUserId,
            StructuralAuditMetadata.ForBlocoInativado(bloco.Id, bloco.Nome),
            $"Bloco '{bloco.Nome}' inativado",
            cancellationToken);
        await blocoRepository.SaveAsync(cancellationToken);

        return Result<BlocoDto>.Success(ToDto(bloco));
    }

    private static BlocoDto ToDto(Bloco bloco)
    {
        return new BlocoDto(bloco.Id, bloco.CondominioId, bloco.Nome, bloco.Ativo, bloco.InativadoEm);
    }
}
