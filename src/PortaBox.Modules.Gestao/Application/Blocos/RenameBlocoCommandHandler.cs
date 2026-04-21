using FluentValidation;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed class RenameBlocoCommandHandler(
    IValidator<RenameBlocoCommand> validator,
    IBlocoRepository blocoRepository,
    IAuditService auditService,
    TimeProvider timeProvider) : ICommandHandler<RenameBlocoCommand, BlocoDto>
{
    public async Task<Result<BlocoDto>> HandleAsync(RenameBlocoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var bloco = await blocoRepository.GetByIdAsync(command.BlocoId, cancellationToken);
        if (bloco is null || bloco.CondominioId != command.CondominioId)
        {
            return Result<BlocoDto>.Failure("Bloco nao encontrado");
        }

        var normalizedNome = command.Nome.Trim();
        if (string.Equals(bloco.Nome, normalizedNome, StringComparison.Ordinal))
        {
            return Result<BlocoDto>.Failure("O novo nome do bloco deve ser diferente do nome atual.");
        }

        if (await blocoRepository.ExistsActiveWithNameAsync(command.CondominioId, normalizedNome, cancellationToken))
        {
            return Result<BlocoDto>.Failure("Ja existe bloco ativo com este nome");
        }

        var nomeAnterior = bloco.Nome;
        var renameResult = bloco.Rename(normalizedNome, command.PerformedByUserId, timeProvider.GetUtcNow().UtcDateTime);
        if (!renameResult.IsSuccess)
        {
            return Result<BlocoDto>.Failure(renameResult.Error ?? "Nao foi possivel renomear o bloco.");
        }

        await auditService.RecordStructuralAsync(
            TenantAuditEventKind.BlocoRenomeado,
            bloco.TenantId,
            command.PerformedByUserId,
            StructuralAuditMetadata.ForBlocoRenomeado(bloco.Id, nomeAnterior, bloco.Nome),
            $"Bloco '{nomeAnterior}' renomeado para '{bloco.Nome}'",
            cancellationToken);
        await blocoRepository.SaveAsync(cancellationToken);

        return Result<BlocoDto>.Success(ToDto(bloco));
    }

    private static BlocoDto ToDto(Bloco bloco)
    {
        return new BlocoDto(bloco.Id, bloco.CondominioId, bloco.Nome, bloco.Ativo, bloco.InativadoEm);
    }
}
