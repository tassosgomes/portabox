using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Domain.Blocos;

namespace PortaBox.Infrastructure.Repositories;

public sealed class BlocoRepository(AppDbContext dbContext) : IBlocoRepository
{
    public Task<Bloco?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Blocos.FirstOrDefaultAsync(bloco => bloco.Id == id, cancellationToken);
    }

    public Task<Bloco?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Blocos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(bloco => bloco.Id == id, cancellationToken);
    }

    public Task<bool> ExistsActiveWithNameAsync(Guid condominioId, string nome, CancellationToken cancellationToken = default)
    {
        return dbContext.Blocos.AnyAsync(
            bloco => bloco.CondominioId == condominioId && bloco.Nome == nome,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Bloco>> ListByCondominioAsync(
        Guid condominioId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        if (!includeInactive)
        {
            return await dbContext.Blocos
                .Where(bloco => bloco.CondominioId == condominioId)
                .OrderBy(bloco => bloco.Nome)
                .ToListAsync(cancellationToken);
        }

        var tenantId = dbContext.CurrentTenantId;
        if (tenantId is null)
        {
            return [];
        }

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, tenant_id, condominio_id, nome, ativo, inativado_em, inativado_por, criado_em, criado_por
                FROM bloco
                WHERE tenant_id = @tenantId AND condominio_id = @condominioId
                ORDER BY nome
                """;

            var tenantParameter = command.CreateParameter();
            tenantParameter.ParameterName = "@tenantId";
            tenantParameter.Value = tenantId.Value;
            command.Parameters.Add(tenantParameter);

            var condominioParameter = command.CreateParameter();
            condominioParameter.ParameterName = "@condominioId";
            condominioParameter.Value = condominioId;
            command.Parameters.Add(condominioParameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var blocos = new List<Bloco>();

            while (await reader.ReadAsync(cancellationToken))
            {
                blocos.Add(MapBloco(reader));
            }

            return blocos;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    public Task AddAsync(Bloco bloco, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bloco);

        return dbContext.Blocos.AddAsync(bloco, cancellationToken).AsTask();
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Bloco MapBloco(IDataRecord record)
    {
        return Bloco.Rehydrate(
            record.GetGuid(0),
            record.GetGuid(1),
            record.GetGuid(2),
            record.GetString(3),
            record.GetBoolean(4),
            record.IsDBNull(5) ? null : record.GetDateTime(5),
            record.IsDBNull(6) ? null : record.GetGuid(6),
            ReadDateTimeOffset(record, 7),
            record.GetGuid(8));
    }

    private static DateTimeOffset ReadDateTimeOffset(IDataRecord record, int ordinal)
    {
        var value = record.GetValue(ordinal);

        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime, TimeSpan.Zero),
            string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            _ => throw new InvalidOperationException($"Unsupported datetime value type '{value.GetType().FullName}' for bloco row materialization.")
        };
    }
}
