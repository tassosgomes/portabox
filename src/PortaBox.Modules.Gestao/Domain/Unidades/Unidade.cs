using System.Text.RegularExpressions;
using PortaBox.Application.Abstractions;
using PortaBox.Domain;
using PortaBox.Domain.Abstractions;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain.Unidades.Events;

namespace PortaBox.Modules.Gestao.Domain.Unidades;

public sealed class Unidade : SoftDeleteableAggregateRoot, ITenantEntity
{
    private static readonly Regex NumeroPattern = new("^[0-9]{1,4}[A-Z]?$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private Unidade()
    {
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid BlocoId { get; private set; }

    public int Andar { get; private set; }

    public string Numero { get; private set; } = string.Empty;

    public DateTimeOffset CriadoEm { get; private set; }

    public Guid CriadoPor { get; private set; }

    public static Result<Unidade> Create(
        Guid id,
        Guid tenantId,
        Bloco bloco,
        int andar,
        string numero,
        Guid porUserId,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(bloco);
        ArgumentNullException.ThrowIfNull(clock);

        if (bloco.TenantId != tenantId)
        {
            return Result<Unidade>.Failure("Inconsistencia de tenant entre bloco e unidade.");
        }

        if (!bloco.Ativo)
        {
            return Result<Unidade>.Failure("Nao e possivel criar unidade em bloco inativo.");
        }

        if (andar < 0)
        {
            return Result<Unidade>.Failure("O andar da unidade deve ser maior ou igual a zero.");
        }

        var normalizedNumero = NormalizeNumero(numero);
        if (normalizedNumero is null)
        {
            return Result<Unidade>.Failure("O numero da unidade deve seguir o formato de 1 a 4 digitos com sufixo alfabetico opcional.");
        }

        var unidade = new Unidade
        {
            Id = id,
            TenantId = tenantId,
            BlocoId = bloco.Id,
            Andar = andar,
            Numero = normalizedNumero,
            CriadoEm = clock.GetUtcNow(),
            CriadoPor = porUserId
        };

        unidade.AddDomainEvent(new UnidadeCriadaV1(
            Guid.NewGuid(),
            unidade.TenantId,
            unidade.Id,
            unidade.BlocoId,
            unidade.Andar,
            unidade.Numero,
            unidade.CriadoPor,
            unidade.CriadoEm));

        return Result<Unidade>.Success(unidade);
    }

    public new Result Inativar(Guid porUserId, DateTime agoraUtc)
    {
        var result = base.Inativar(porUserId, agoraUtc);
        if (!result.IsSuccess)
        {
            return result;
        }

        AddDomainEvent(new UnidadeInativadaV1(
            Guid.NewGuid(),
            TenantId,
            Id,
            BlocoId,
            Andar,
            Numero,
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

        AddDomainEvent(new UnidadeReativadaV1(
            Guid.NewGuid(),
            TenantId,
            Id,
            BlocoId,
            Andar,
            Numero,
            porUserId,
            new DateTimeOffset(agoraUtc, TimeSpan.Zero)));

        return Result.Success();
    }

    private static string? NormalizeNumero(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            return null;
        }

        var normalizedNumero = numero.Trim().ToUpperInvariant();
        return NumeroPattern.IsMatch(normalizedNumero) ? normalizedNumero : null;
    }
}
