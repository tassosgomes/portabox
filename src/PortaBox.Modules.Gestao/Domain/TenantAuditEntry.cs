namespace PortaBox.Modules.Gestao.Domain;

public sealed class TenantAuditEntry
{
    private TenantAuditEntry()
    {
    }

    public long Id { get; private set; }

    public Guid TenantId { get; private set; }

    public TenantAuditEventKind EventKind { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string? Note { get; private set; }

    public string? MetadataJson { get; private set; }

    public Condominio? Condominio { get; private set; }

    public static TenantAuditEntry Create(
        Guid tenantId,
        TenantAuditEventKind eventKind,
        Guid performedByUserId,
        DateTimeOffset occurredAt,
        string? note = null,
        string? metadataJson = null)
    {
        return new TenantAuditEntry
        {
            TenantId = tenantId,
            EventKind = eventKind,
            PerformedByUserId = performedByUserId,
            OccurredAt = occurredAt,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim()
        };
    }
}
