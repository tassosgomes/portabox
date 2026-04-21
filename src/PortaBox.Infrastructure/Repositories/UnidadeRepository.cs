using Microsoft.EntityFrameworkCore;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Unidades;
using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Infrastructure.Repositories;

public sealed class UnidadeRepository(AppDbContext dbContext) : IUnidadeRepository
{
    public Task<Unidade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Unidade>().FirstOrDefaultAsync(unidade => unidade.Id == id, cancellationToken);
    }

    public Task<Unidade?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<Unidade>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(unidade => unidade.Id == id, cancellationToken);
    }

    public Task<Unidade?> FindActiveByCanonicalAsync(
        Guid tenantId,
        Guid blocoId,
        int andar,
        string numero,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumero = NormalizeNumero(numero);
        if (normalizedNumero is null)
        {
            return Task.FromResult<Unidade?>(null);
        }

        return dbContext.Set<Unidade>().FirstOrDefaultAsync(
            unidade => unidade.TenantId == tenantId
                && unidade.BlocoId == blocoId
                && unidade.Andar == andar
                && unidade.Numero == normalizedNumero,
            cancellationToken);
    }

    public Task<bool> ExistsActiveWithCanonicalAsync(
        Guid tenantId,
        Guid blocoId,
        int andar,
        string numero,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumero = NormalizeNumero(numero);
        if (normalizedNumero is null)
        {
            return Task.FromResult(false);
        }

        return dbContext.Set<Unidade>().AnyAsync(
            unidade => unidade.TenantId == tenantId
                && unidade.BlocoId == blocoId
                && unidade.Andar == andar
                && unidade.Numero == normalizedNumero,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Unidade>> ListByBlocoAsync(
        Guid blocoId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        if (!includeInactive)
        {
            return await dbContext.Set<Unidade>()
                .Where(unidade => unidade.BlocoId == blocoId)
                .OrderBy(unidade => unidade.Andar)
                .ThenBy(unidade => unidade.Numero)
                .ToListAsync(cancellationToken);
        }

        var tenantId = dbContext.CurrentTenantId;
        if (tenantId is null)
        {
            return [];
        }

        return await dbContext.Set<Unidade>()
            .IgnoreQueryFilters()
            .Where(unidade => unidade.TenantId == tenantId && unidade.BlocoId == blocoId)
            .OrderBy(unidade => unidade.Andar)
            .ThenBy(unidade => unidade.Numero)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Unidade>> ListByCondominioAsync(
        Guid condominioId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        if (!includeInactive)
        {
            return await dbContext.Set<Unidade>()
                .Join(
                    dbContext.Blocos,
                    unidade => unidade.BlocoId,
                    bloco => bloco.Id,
                    (unidade, bloco) => new { Unidade = unidade, bloco.CondominioId })
                .Where(current => current.CondominioId == condominioId)
                .OrderBy(current => current.Unidade.Andar)
                .ThenBy(current => current.Unidade.Numero)
                .Select(current => current.Unidade)
                .ToListAsync(cancellationToken);
        }

        var tenantId = dbContext.CurrentTenantId;
        if (tenantId is null)
        {
            return [];
        }

        return await dbContext.Set<Unidade>()
            .IgnoreQueryFilters()
            .Join(
                dbContext.Blocos.IgnoreQueryFilters(),
                unidade => unidade.BlocoId,
                bloco => bloco.Id,
                (unidade, bloco) => new { Unidade = unidade, Bloco = bloco })
            .Where(current => current.Unidade.TenantId == tenantId.Value
                && current.Bloco.TenantId == tenantId.Value
                && current.Bloco.CondominioId == condominioId)
            .OrderBy(current => current.Unidade.Andar)
            .ThenBy(current => current.Unidade.Numero)
            .Select(current => current.Unidade)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Unidade unidade, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unidade);

        return dbContext.Set<Unidade>().AddAsync(unidade, cancellationToken).AsTask();
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeNumero(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            return null;
        }

        return numero.Trim().ToUpperInvariant();
    }
}
