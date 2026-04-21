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

public sealed class CreateUnidadeCommandHandler(
    IValidator<CreateUnidadeCommand> validator,
    IBlocoRepository blocoRepository,
    IUnidadeRepository unidadeRepository,
    IAuditService auditService,
    TimeProvider timeProvider) : ICommandHandler<CreateUnidadeCommand, UnidadeDto>
{
    public async Task<Result<UnidadeDto>> HandleAsync(CreateUnidadeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var bloco = await blocoRepository.GetByIdIncludingInactiveAsync(command.BlocoId, cancellationToken);
        if (bloco is null || bloco.CondominioId != command.CondominioId)
        {
            return Result<UnidadeDto>.Failure("Bloco nao encontrado");
        }

        if (!bloco.Ativo)
        {
            return Result<UnidadeDto>.Failure("Bloco inativo");
        }

        var normalizedNumero = command.Numero.Trim().ToUpperInvariant();
        if (await unidadeRepository.ExistsActiveWithCanonicalAsync(bloco.TenantId, bloco.Id, command.Andar, normalizedNumero, cancellationToken))
        {
            return Result<UnidadeDto>.Failure("Unidade ja existe");
        }

        var createResult = Unidade.Create(
            Guid.NewGuid(),
            bloco.TenantId,
            bloco,
            command.Andar,
            normalizedNumero,
            command.PerformedByUserId,
            timeProvider);

        if (!createResult.IsSuccess || createResult.Value is null)
        {
            return Result<UnidadeDto>.Failure(createResult.Error ?? "Nao foi possivel criar a unidade.");
        }

        try
        {
            await unidadeRepository.AddAsync(createResult.Value, cancellationToken);
            await auditService.RecordStructuralAsync(
                TenantAuditEventKind.UnidadeCriada,
                createResult.Value.TenantId,
                command.PerformedByUserId,
                StructuralAuditMetadata.ForUnidadeCriada(
                    createResult.Value.Id,
                    createResult.Value.BlocoId,
                    createResult.Value.Andar,
                    createResult.Value.Numero),
                $"Unidade '{createResult.Value.Numero}' criada no bloco '{bloco.Nome}'",
                cancellationToken);
            await unidadeRepository.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Result<UnidadeDto>.Failure("Unidade ja existe");
        }

        return Result<UnidadeDto>.Success(ToDto(createResult.Value));
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
