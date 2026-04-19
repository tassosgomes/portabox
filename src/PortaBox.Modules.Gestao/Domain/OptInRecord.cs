using PortaBox.Domain.Abstractions;
using PortaBox.Modules.Gestao.Application.Validators;

namespace PortaBox.Modules.Gestao.Domain;

public sealed class OptInRecord : ITenantEntity
{
    private OptInRecord()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public DateOnly DataAssembleia { get; private set; }

    public string QuorumDescricao { get; private set; } = string.Empty;

    public string SignatarioNome { get; private set; } = string.Empty;

    public string SignatarioCpf { get; private set; } = string.Empty;

    public DateOnly DataTermo { get; private set; }

    public Guid RegisteredByUserId { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public Condominio? Condominio { get; private set; }

    public static OptInRecord Create(
        Guid id,
        Guid tenantId,
        DateOnly dataAssembleia,
        string quorumDescricao,
        string signatarioNome,
        string signatarioCpf,
        DateOnly dataTermo,
        Guid registeredByUserId,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quorumDescricao);
        ArgumentException.ThrowIfNullOrWhiteSpace(signatarioNome);
        ArgumentNullException.ThrowIfNull(clock);

        if (dataAssembleia == default)
        {
            throw new ArgumentOutOfRangeException(nameof(dataAssembleia), "Assembly date must be provided.");
        }

        if (dataTermo == default)
        {
            throw new ArgumentOutOfRangeException(nameof(dataTermo), "Term date must be provided.");
        }

        var normalizedCpf = CpfValidator.Normalize(signatarioCpf);

        if (!CpfValidator.IsValid(normalizedCpf))
        {
            throw new ArgumentException("CPF must be valid.", nameof(signatarioCpf));
        }

        return new OptInRecord
        {
            Id = id,
            TenantId = tenantId,
            DataAssembleia = dataAssembleia,
            QuorumDescricao = quorumDescricao.Trim(),
            SignatarioNome = signatarioNome.Trim(),
            SignatarioCpf = normalizedCpf,
            DataTermo = dataTermo,
            RegisteredByUserId = registeredByUserId,
            RegisteredAt = clock.GetUtcNow()
        };
    }
}
