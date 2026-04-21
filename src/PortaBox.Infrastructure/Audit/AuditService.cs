using System.Text.Json;
using PortaBox.Infrastructure.Persistence;
using PortaBox.Modules.Gestao.Application.Audit;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Infrastructure.Audit;

public sealed class AuditService(AppDbContext dbContext, TimeProvider timeProvider) : IAuditService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task RecordStructuralAsync(
        TenantAuditEventKind kind,
        Guid tenantId,
        Guid performedByUserId,
        IDictionary<string, object> metadata,
        string? note,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var auditEntry = TenantAuditEntry.Create(
            tenantId,
            kind,
            performedByUserId,
            timeProvider.GetUtcNow(),
            note,
            JsonSerializer.Serialize(metadata, JsonSerializerOptions));

        await dbContext.TenantAuditEntries.AddAsync(auditEntry, cancellationToken);
    }
}
