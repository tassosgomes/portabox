using PortaBox.Domain.Abstractions;

namespace PortaBox.Modules.Gestao.Domain;

public sealed class Sindico : ITenantEntity
{
    private Sindico()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string NomeCompleto { get; private set; } = string.Empty;

    public string CelularE164 { get; private set; } = string.Empty;

    public SindicoStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Condominio? Condominio { get; private set; }

    public static Sindico Create(
        Guid id,
        Guid tenantId,
        Guid userId,
        string nomeCompleto,
        string celularE164,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeCompleto);
        ArgumentException.ThrowIfNullOrWhiteSpace(celularE164);
        ArgumentNullException.ThrowIfNull(clock);

        return new Sindico
        {
            Id = id,
            TenantId = tenantId,
            UserId = userId,
            NomeCompleto = nomeCompleto.Trim(),
            CelularE164 = celularE164.Trim(),
            Status = SindicoStatus.Ativo,
            CreatedAt = clock.GetUtcNow()
        };
    }
}
