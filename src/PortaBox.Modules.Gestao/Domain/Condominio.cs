using PortaBox.Domain.Abstractions;
using PortaBox.Modules.Gestao.Application.Validators;
using PortaBox.Modules.Gestao.Domain.Events;

namespace PortaBox.Modules.Gestao.Domain;

public sealed class Condominio : AggregateRoot
{
    private Condominio()
    {
    }

    public Guid Id { get; private set; }

    public string NomeFantasia { get; private set; } = string.Empty;

    public string Cnpj { get; private set; } = string.Empty;

    public string? EnderecoLogradouro { get; private set; }

    public string? EnderecoNumero { get; private set; }

    public string? EnderecoComplemento { get; private set; }

    public string? EnderecoBairro { get; private set; }

    public string? EnderecoCidade { get; private set; }

    public string? EnderecoUf { get; private set; }

    public string? EnderecoCep { get; private set; }

    public string? AdministradoraNome { get; private set; }

    public CondominioStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset? ActivatedAt { get; private set; }

    public Guid? ActivatedByUserId { get; private set; }

    public OptInRecord? OptInRecord { get; private set; }

    public List<OptInDocument> OptInDocuments { get; private set; } = [];

    public List<Sindico> Sindicos { get; private set; } = [];

    public List<TenantAuditEntry> TenantAuditEntries { get; private set; } = [];

    public static Condominio Create(
        Guid id,
        string nomeFantasia,
        string cnpj,
        Guid createdByUserId,
        TimeProvider clock,
        string? enderecoLogradouro = null,
        string? enderecoNumero = null,
        string? enderecoComplemento = null,
        string? enderecoBairro = null,
        string? enderecoCidade = null,
        string? enderecoUf = null,
        string? enderecoCep = null,
        string? administradoraNome = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeFantasia);
        ArgumentNullException.ThrowIfNull(clock);

        var normalizedCnpj = CnpjValidator.Normalize(cnpj);

        if (!CnpjValidator.IsValid(normalizedCnpj))
        {
            throw new ArgumentException("CNPJ must be valid.", nameof(cnpj));
        }

        return new Condominio
        {
            Id = id,
            NomeFantasia = nomeFantasia.Trim(),
            Cnpj = normalizedCnpj,
            EnderecoLogradouro = NormalizeOptional(enderecoLogradouro),
            EnderecoNumero = NormalizeOptional(enderecoNumero),
            EnderecoComplemento = NormalizeOptional(enderecoComplemento),
            EnderecoBairro = NormalizeOptional(enderecoBairro),
            EnderecoCidade = NormalizeOptional(enderecoCidade),
            EnderecoUf = NormalizeOptional(enderecoUf)?.ToUpperInvariant(),
            EnderecoCep = NormalizeOptional(enderecoCep),
            AdministradoraNome = NormalizeOptional(administradoraNome),
            Status = CondominioStatus.PreAtivo,
            CreatedAt = clock.GetUtcNow(),
            CreatedByUserId = createdByUserId
        };
    }

    public void RaiseCadastrado(
        Guid sindicoUserId,
        string sindicoNomeCompleto,
        string sindicoEmail,
        DateTimeOffset occurredAt)
    {
        AddDomainEvent(new CondominioCadastradoV1(
            Id,
            sindicoUserId,
            NomeFantasia,
            Cnpj,
            sindicoNomeCompleto.Trim(),
            sindicoEmail.Trim(),
            occurredAt));
    }

    public bool TryActivate(Guid performedByUserId, TimeProvider clock, out CondominioAtivadoV1? domainEvent)
    {
        ArgumentNullException.ThrowIfNull(clock);

        domainEvent = null;

        if (Status == CondominioStatus.Ativo)
        {
            return false;
        }

        var occurredAt = clock.GetUtcNow();
        Status = CondominioStatus.Ativo;
        ActivatedAt = occurredAt;
        ActivatedByUserId = performedByUserId;

        var activatedEvent = new CondominioAtivadoV1(
            Id,
            performedByUserId,
            occurredAt);

        domainEvent = activatedEvent;
        AddDomainEvent(activatedEvent);
        return true;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
