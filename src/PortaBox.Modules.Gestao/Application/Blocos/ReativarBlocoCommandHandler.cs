using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed class ReativarBlocoCommandHandler(
    IValidator<ReativarBlocoCommand> validator,
    IBlocoRepository blocoRepository,
    IAuditService auditService,
    TimeProvider timeProvider) : ICommandHandler<ReativarBlocoCommand, BlocoDto>
{
    public async Task<Result<BlocoDto>> HandleAsync(ReativarBlocoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var bloco = await blocoRepository.GetByIdIncludingInactiveAsync(command.BlocoId, cancellationToken);
        if (bloco is null || bloco.CondominioId != command.CondominioId)
        {
            return Result<BlocoDto>.Failure("Bloco nao encontrado");
        }

        if (bloco.Ativo)
        {
            var alreadyActiveResult = bloco.Reativar(command.PerformedByUserId, timeProvider.GetUtcNow().UtcDateTime);
            return Result<BlocoDto>.Failure(alreadyActiveResult.Error ?? "Nao foi possivel reativar o bloco.");
        }

        if (await blocoRepository.ExistsActiveWithNameAsync(command.CondominioId, bloco.Nome, cancellationToken))
        {
            return Result<BlocoDto>.Failure("Ja existe bloco ativo com este nome; conflito canonico, inative o outro antes");
        }

        var reativarResult = bloco.Reativar(command.PerformedByUserId, timeProvider.GetUtcNow().UtcDateTime);
        if (!reativarResult.IsSuccess)
        {
            return Result<BlocoDto>.Failure(reativarResult.Error ?? "Nao foi possivel reativar o bloco.");
        }

        try
        {
            await auditService.RecordStructuralAsync(
                TenantAuditEventKind.BlocoReativado,
                bloco.TenantId,
                command.PerformedByUserId,
                StructuralAuditMetadata.ForBlocoReativado(bloco.Id, bloco.Nome),
                $"Bloco '{bloco.Nome}' reativado",
                cancellationToken);
            await blocoRepository.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Result<BlocoDto>.Failure("Ja existe bloco ativo com este nome");
        }

        return Result<BlocoDto>.Success(ToDto(bloco));
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

    private static BlocoDto ToDto(Bloco bloco)
    {
        return new BlocoDto(bloco.Id, bloco.CondominioId, bloco.Nome, bloco.Ativo, bloco.InativadoEm);
    }
}
