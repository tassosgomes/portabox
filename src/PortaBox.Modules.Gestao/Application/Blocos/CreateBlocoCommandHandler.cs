using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed class CreateBlocoCommandHandler(
    IValidator<CreateBlocoCommand> validator,
    IBlocoRepository blocoRepository,
    IAuditService auditService,
    TimeProvider timeProvider) : ICommandHandler<CreateBlocoCommand, BlocoDto>
{
    public async Task<Result<BlocoDto>> HandleAsync(CreateBlocoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var normalizedNome = command.Nome.Trim();
        if (await blocoRepository.ExistsActiveWithNameAsync(command.CondominioId, normalizedNome, cancellationToken))
        {
            return Result<BlocoDto>.Failure("Ja existe bloco ativo com este nome");
        }

        var createResult = Bloco.Create(
            Guid.NewGuid(),
            command.CondominioId,
            command.CondominioId,
            normalizedNome,
            command.PerformedByUserId,
            timeProvider);

        if (!createResult.IsSuccess || createResult.Value is null)
        {
            return Result<BlocoDto>.Failure(createResult.Error ?? "Nao foi possivel criar o bloco.");
        }

        try
        {
            await blocoRepository.AddAsync(createResult.Value, cancellationToken);
            await auditService.RecordStructuralAsync(
                TenantAuditEventKind.BlocoCriado,
                createResult.Value.TenantId,
                command.PerformedByUserId,
                StructuralAuditMetadata.ForBlocoCriado(createResult.Value.Id, createResult.Value.Nome),
                $"Bloco '{createResult.Value.Nome}' criado",
                cancellationToken);
            await blocoRepository.SaveAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Result<BlocoDto>.Failure("Ja existe bloco ativo com este nome");
        }

        return Result<BlocoDto>.Success(ToDto(createResult.Value));
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
