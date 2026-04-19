using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain;

public sealed class OptInDocument : ITenantEntity
{
    private OptInDocument()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public OptInDocumentKind Kind { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string Sha256 { get; private set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public Condominio? Condominio { get; private set; }

    public static OptInDocument Create(
        Guid id,
        Guid tenantId,
        OptInDocumentKind kind,
        string storageKey,
        string contentType,
        long sizeBytes,
        string sha256,
        Guid uploadedByUserId,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        ArgumentNullException.ThrowIfNull(clock);

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Document size must be greater than zero.");
        }

        if (sha256.Length != 64)
        {
            throw new ArgumentException("SHA-256 hash must be a 64-character hexadecimal string.", nameof(sha256));
        }

        return new OptInDocument
        {
            Id = id,
            TenantId = tenantId,
            Kind = kind,
            StorageKey = storageKey.Trim(),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            Sha256 = sha256.Trim().ToLowerInvariant(),
            UploadedAt = clock.GetUtcNow(),
            UploadedByUserId = uploadedByUserId
        };
    }
}
