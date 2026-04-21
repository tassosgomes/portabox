using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Audit;

public interface IAuditService
{
    Task RecordStructuralAsync(
        TenantAuditEventKind kind,
        Guid tenantId,
        Guid performedByUserId,
        IDictionary<string, object> metadata,
        string? note,
        CancellationToken cancellationToken);
}
