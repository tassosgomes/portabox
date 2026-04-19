using Microsoft.EntityFrameworkCore;
using PortaBox.Modules.Gestao.Application.Common;
using PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;
using PortaBox.Modules.Gestao.Application.Queries.ListCondominios;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Application.Validators;
using PortaBox.Modules.Gestao.Domain;
using PortaBox.Infrastructure.Identity;
using PortaBox.Infrastructure.Persistence;

namespace PortaBox.Infrastructure.Repositories;

public sealed class CondominioRepository(AppDbContext dbContext) : ICondominioRepository
{

    public Task AddAsync(Condominio condominio, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condominio);

        return dbContext.Condominios.AddAsync(condominio, cancellationToken).AsTask();
    }

    public Task<Condominio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Condominios.FirstOrDefaultAsync(condominio => condominio.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        var normalizedCnpj = CnpjValidator.Normalize(cnpj);

        return dbContext.Condominios.AnyAsync(condominio => condominio.Cnpj == normalizedCnpj, cancellationToken);
    }

    public async Task<CondominioDetailsDto?> GetDetailsAsync(
        Guid id,
        bool ignoreTenantFilter,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!ignoreTenantFilter && tenantId.HasValue && tenantId.Value != id)
        {
            return null;
        }

        var query = dbContext.Condominios
            .AsNoTracking()
            .AsSplitQuery()
            .Include(condominio => condominio.OptInRecord)
            .Include(condominio => condominio.OptInDocuments)
            .Include(condominio => condominio.Sindicos)
            .Include(condominio => condominio.TenantAuditEntries
                .OrderByDescending(entry => entry.OccurredAt)
                .Take(20))
            .Where(condominio => condominio.Id == id);

        if (ignoreTenantFilter)
        {
            query = query.IgnoreQueryFilters();
        }

        var condominio = await query.SingleOrDefaultAsync(cancellationToken);
        if (condominio is null)
        {
            return null;
        }

        var sindico = condominio.Sindicos
            .OrderByDescending(current => current.CreatedAt)
            .FirstOrDefault();

        var sindicoUser = sindico is null
            ? null
            : await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == sindico.UserId)
                .Select(user => new
                {
                    user.Email,
                    HasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash)
                })
                .SingleOrDefaultAsync(cancellationToken);

        return new CondominioDetailsDto(
            condominio.Id,
            condominio.NomeFantasia,
            Masking.Cnpj(condominio.Cnpj),
            condominio.Status,
            condominio.CreatedAt,
            condominio.ActivatedAt,
            condominio.EnderecoLogradouro,
            condominio.EnderecoNumero,
            condominio.EnderecoComplemento,
            condominio.EnderecoBairro,
            condominio.EnderecoCidade,
            condominio.EnderecoUf,
            condominio.EnderecoCep,
            condominio.AdministradoraNome,
            condominio.OptInRecord is null
                ? null
                : new CondominioOptInDetailsDto(
                    condominio.OptInRecord.DataAssembleia,
                    condominio.OptInRecord.QuorumDescricao,
                    condominio.OptInRecord.SignatarioNome,
                    Masking.Cpf(condominio.OptInRecord.SignatarioCpf),
                    condominio.OptInRecord.DataTermo,
                    condominio.OptInRecord.RegisteredByUserId,
                    condominio.OptInRecord.RegisteredAt),
            sindico is null
                ? null
                : new CondominioSindicoDetailsDto(
                    sindico.Id,
                    sindico.UserId,
                    sindico.NomeCompleto,
                    sindicoUser?.Email ?? string.Empty,
                    Masking.Celular(sindico.CelularE164),
                    sindico.Status,
                    sindico.CreatedAt),
            condominio.OptInDocuments
                .OrderByDescending(document => document.UploadedAt)
                .Select(document => new CondominioDocumentDetailsDto(
                    document.Id,
                    document.Kind,
                    document.ContentType,
                    document.SizeBytes,
                    document.Sha256,
                    document.UploadedAt,
                    document.UploadedByUserId))
                .ToArray(),
            condominio.TenantAuditEntries
                .OrderByDescending(entry => entry.OccurredAt)
                .Take(20)
                .Select(entry => new CondominioAuditLogItemDto(
                    entry.Id,
                    entry.EventKind,
                    entry.PerformedByUserId,
                    entry.OccurredAt,
                    entry.Note,
                    entry.MetadataJson))
                .ToArray(),
            sindicoUser?.HasPassword ?? false);
    }

    public async Task<PagedResult<CondominioListItemDto>> ListAsync(
        ListCondominiosQuery query,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var condominios = dbContext.Condominios.AsNoTracking().AsQueryable();

        if (tenantId.HasValue)
        {
            condominios = condominios.Where(condominio => condominio.Id == tenantId.Value);
        }

        if (query.Status.HasValue)
        {
            condominios = condominios.Where(condominio => condominio.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchPattern = $"%{query.SearchTerm}%";
            var normalizedDigits = new string(query.SearchTerm.Where(char.IsDigit).ToArray());

            condominios = condominios.Where(condominio =>
                EF.Functions.ILike(condominio.NomeFantasia, searchPattern) ||
                (!string.IsNullOrWhiteSpace(normalizedDigits) && EF.Functions.ILike(condominio.Cnpj, $"%{normalizedDigits}%")));
        }

        var totalCount = await condominios.CountAsync(cancellationToken);

        var items = await condominios
            .OrderByDescending(condominio => condominio.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(condominio => new CondominioListItemDto(
                condominio.Id,
                condominio.NomeFantasia,
                Masking.Cnpj(condominio.Cnpj),
                condominio.Status,
                condominio.CreatedAt,
                condominio.ActivatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CondominioListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    public Task UpdateAsync(Condominio condominio, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(condominio);

        dbContext.Condominios.Update(condominio);
        return Task.CompletedTask;
    }
}
