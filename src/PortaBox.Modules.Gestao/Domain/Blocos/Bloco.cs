using PortaBox.Application.Abstractions;
using PortaBox.Domain;
using PortaBox.Domain.Abstractions;
using PortaBox.Modules.Gestao.Domain.Blocos.Events;

namespace PortaBox.Modules.Gestao.Domain.Blocos;

public sealed class Bloco : SoftDeleteableAggregateRoot, ITenantEntity
{
    private Bloco()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CondominioId { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public DateTimeOffset CriadoEm { get; private set; }

    public Guid CriadoPor { get; private set; }

    internal static Bloco Rehydrate(
        Guid id,
        Guid tenantId,
        Guid condominioId,
        string nome,
        bool ativo,
        DateTime? inativadoEm,
        Guid? inativadoPor,
        DateTimeOffset criadoEm,
        Guid criadoPor)
    {
        return new Bloco
        {
            Id = id,
            TenantId = tenantId,
            CondominioId = condominioId,
            Nome = nome,
            Ativo = ativo,
            InativadoEm = inativadoEm,
            InativadoPor = inativadoPor,
            CriadoEm = criadoEm,
            CriadoPor = criadoPor
        };
    }

    public static Result<Bloco> Create(
        Guid id,
        Guid tenantId,
        Guid condominioId,
        string nome,
        Guid porUserId,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var normalizedNome = NormalizeNome(nome);
        if (normalizedNome is null)
        {
            return Result<Bloco>.Failure("O nome do bloco deve ter entre 1 e 50 caracteres.");
        }

        var bloco = new Bloco
        {
            Id = id,
            TenantId = tenantId,
            CondominioId = condominioId,
            Nome = normalizedNome,
            CriadoEm = clock.GetUtcNow(),
            CriadoPor = porUserId
        };

        bloco.AddDomainEvent(new BlocoCriadoV1(
            Guid.NewGuid(),
            bloco.TenantId,
            bloco.Id,
            bloco.CondominioId,
            bloco.Nome,
            bloco.CriadoPor,
            bloco.CriadoEm));

        return Result<Bloco>.Success(bloco);
    }

    public Result Rename(string novoNome, Guid porUserId, DateTime agoraUtc)
    {
        var normalizedNome = NormalizeNome(novoNome);
        if (normalizedNome is null)
        {
            return Result.Failure("O nome do bloco deve ter entre 1 e 50 caracteres.");
        }

        if (!Ativo)
        {
            return Result.Failure("Nao e possivel renomear bloco inativo.");
        }

        if (string.Equals(Nome, normalizedNome, StringComparison.Ordinal))
        {
            return Result.Failure("O novo nome do bloco deve ser diferente do nome atual.");
        }

        var nomeAnterior = Nome;
        Nome = normalizedNome;

        AddDomainEvent(new BlocoRenomeadoV1(
            Guid.NewGuid(),
            TenantId,
            Id,
            nomeAnterior,
            Nome,
            porUserId,
            new DateTimeOffset(agoraUtc, TimeSpan.Zero)));

        return Result.Success();
    }

    public new Result Inativar(Guid porUserId, DateTime agoraUtc)
    {
        var result = base.Inativar(porUserId, agoraUtc);
        if (!result.IsSuccess)
        {
            return result;
        }

        AddDomainEvent(new BlocoInativadoV1(
            Guid.NewGuid(),
            TenantId,
            Id,
            Nome,
            porUserId,
            new DateTimeOffset(agoraUtc, TimeSpan.Zero)));

        return Result.Success();
    }

    public new Result Reativar(Guid porUserId, DateTime agoraUtc)
    {
        var result = base.Reativar(porUserId, agoraUtc);
        if (!result.IsSuccess)
        {
            return result;
        }

        AddDomainEvent(new BlocoReativadoV1(
            Guid.NewGuid(),
            TenantId,
            Id,
            Nome,
            porUserId,
            new DateTimeOffset(agoraUtc, TimeSpan.Zero)));

        return Result.Success();
    }

    private static string? NormalizeNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return null;
        }

        var normalizedNome = nome.Trim();
        return normalizedNome.Length is > 0 and <= 50 ? normalizedNome : null;
    }
}
