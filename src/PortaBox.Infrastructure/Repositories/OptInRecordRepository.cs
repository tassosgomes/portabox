using Microsoft.EntityFrameworkCore;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Repositories;

public sealed class OptInRecordRepository(AppDbContext dbContext) : IOptInRecordRepository
{
    public Task AddAsync(OptInRecord optInRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(optInRecord);

        return dbContext.OptInRecords.AddAsync(optInRecord, cancellationToken).AsTask();
    }

    public Task<OptInRecord?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return dbContext.OptInRecords.FirstOrDefaultAsync(
            optInRecord => optInRecord.TenantId == tenantId,
            cancellationToken);
    }
}
