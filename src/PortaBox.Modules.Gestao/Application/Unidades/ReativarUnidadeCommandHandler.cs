using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Modules.Gestao.Application.Unidades;

public sealed class ReativarUnidadeCommandHandler(
    IValidator<ReativarUnidadeCommand> validator,
    IBlocoRepository blocoRepository,
    IUnidadeRepository unidadeRepository,
    IAuditService auditService,
    TimeProvider timeProvider) : ICommandHandler<ReativarUnidadeCommand, UnidadeDto>
{
    public async Task<Result<UnidadeDto>> HandleAsync(ReativarUnidadeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var unidade = await unidadeRepository.GetByIdIncludingInactiveAsync(command.UnidadeId, cancellationToken);
        if (unidade is null || unidade.BlocoId != command.BlocoId)
        {
            return Result<UnidadeDto>.Failure("Unidade nao encontrada");
        }

        var bloco = await blocoRepository.GetByIdIncludingInactiveAsync(command.BlocoId, cancellationToken);
        if (bloco is null || bloco.CondominioId != command.CondominioId)
        {
            return Result<UnidadeDto>.Failure("Bloco nao encontrado");
        }

        if (!bloco.Ativo)
        {
            return Result<UnidadeDto>.Failure("Nao e possivel reativar unidade em bloco inativo.");
        }

        if (!unidade.Ativo && await unidadeRepository.ExistsActiveWithCanonicalAsync(
                unidade.TenantId,
                unidade.BlocoId,
                unidade.Andar,
                unidade.Numero,
                cancellationToken))
        {
            return Result<UnidadeDto>.Failure("Conflito canonico; inative a duplicada antes de reativar esta unidade");
        }

        var reativarResult = unidade.Reativar(command.PerformedByUserId, timeProvider.GetUtcNow().UtcDateTime);
        if (!reativarResult.IsSuccess)
        {
            return Result<UnidadeDto>.Failure(reativarResult.Error ?? "Nao foi possivel reativar a unidade.");
        }

        try
        {
            await auditService.RecordStructuralAsync(
                TenantAuditEventKind.UnidadeReativada,
                unidade.TenantId,
                command.PerformedByUserId,
                StructuralAuditMetadata.ForUnidadeReativada(unidade.Id, unidade.BlocoId, unidade.Andar, unidade.Numero),
                $"Unidade '{unidade.Numero}' reativada",
                cancellationToken);
            await unidadeRepository.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Result<UnidadeDto>.Failure("Conflito canonico; inative a duplicada antes de reativar esta unidade");
        }

        return Result<UnidadeDto>.Success(ToDto(unidade));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            ? string.Equals(postgresException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal)
                || (postgresException.MessageText?.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ?? false)
                || postgresException.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            : exception.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    private static UnidadeDto ToDto(Unidade unidade)
    {
        return new UnidadeDto(unidade.Id, unidade.BlocoId, unidade.Andar, unidade.Numero, unidade.Ativo, unidade.InativadoEm);
    }
}
