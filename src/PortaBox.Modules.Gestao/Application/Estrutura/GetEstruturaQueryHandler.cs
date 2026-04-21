using System.Globalization;
using PortaBox.Application.Abstractions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Application.Unidades;
using PortaBox.Modules.Gestao.Domain.Blocos;
using PortaBox.Modules.Gestao.Domain.Unidades;

namespace PortaBox.Modules.Gestao.Application.Estrutura;

public sealed class GetEstruturaQueryHandler(
    ICondominioRepository condominioRepository,
    IBlocoRepository blocoRepository,
    IUnidadeRepository unidadeRepository,
    ITenantContext tenantContext,
    TimeProvider timeProvider) : IQueryHandler<GetEstruturaQuery, EstruturaDto>
{
    private const string NotFoundError = "Condominio nao encontrado";

    public async Task<Result<EstruturaDto>> HandleAsync(GetEstruturaQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currentTenantId = tenantContext.TenantId;
        if (currentTenantId.HasValue && currentTenantId.Value != query.CondominioId)
        {
            return Result<EstruturaDto>.Failure(NotFoundError);
        }

        var condominio = await condominioRepository.GetByIdAsync(query.CondominioId, cancellationToken);
        if (condominio is null)
        {
            return Result<EstruturaDto>.Failure(NotFoundError);
        }

        var blocos = (await blocoRepository.ListByCondominioAsync(query.CondominioId, query.IncludeInactive, cancellationToken))
            .Where(bloco => bloco.TenantId == condominio.Id && bloco.CondominioId == condominio.Id)
            .OrderBy(bloco => bloco.Nome, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var blocoIds = blocos.Select(bloco => bloco.Id).ToHashSet();
        var unidadesByBloco = (await unidadeRepository.ListByCondominioAsync(query.CondominioId, query.IncludeInactive, cancellationToken))
            .Where(unidade => unidade.TenantId == condominio.Id && blocoIds.Contains(unidade.BlocoId))
            .GroupBy(unidade => unidade.BlocoId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var dto = new EstruturaDto(
            condominio.Id,
            condominio.NomeFantasia,
            blocos.Select(bloco => ToBlocoNodeDto(bloco, unidadesByBloco)).ToArray(),
            timeProvider.GetUtcNow().UtcDateTime);

        return Result<EstruturaDto>.Success(dto);
    }

    private static BlocoNodeDto ToBlocoNodeDto(Bloco bloco, IReadOnlyDictionary<Guid, Unidade[]> unidadesByBloco)
    {
        unidadesByBloco.TryGetValue(bloco.Id, out var unidades);
        unidades ??= [];

        var andares = unidades
            .OrderBy(unidade => unidade.Andar)
            .ThenBy(unidade => unidade.Numero, UnidadeNumeroComparer.Instance)
            .GroupBy(unidade => unidade.Andar)
            .OrderBy(group => group.Key)
            .Select(group => new AndarNodeDto(
                group.Key,
                group
                    .OrderBy(unidade => unidade.Numero, UnidadeNumeroComparer.Instance)
                    .Select(unidade => new UnidadeLeafDto(unidade.Id, unidade.Numero, unidade.Ativo))
                    .ToArray()))
            .ToArray();

        return new BlocoNodeDto(bloco.Id, bloco.Nome, bloco.Ativo, andares);
    }

    private sealed class UnidadeNumeroComparer : IComparer<string>
    {
        public static readonly UnidadeNumeroComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var left = ParseNumero(x);
            var right = ParseNumero(y);

            var numericComparison = left.NumericPart.CompareTo(right.NumericPart);
            if (numericComparison != 0)
            {
                return numericComparison;
            }

            return StringComparer.Ordinal.Compare(left.Suffix, right.Suffix);
        }

        private static (int NumericPart, string Suffix) ParseNumero(string numero)
        {
            var digitCount = 0;
            while (digitCount < numero.Length && char.IsDigit(numero[digitCount]))
            {
                digitCount++;
            }

            if (digitCount == 0 || !int.TryParse(numero[..digitCount], NumberStyles.None, CultureInfo.InvariantCulture, out var numericPart))
            {
                return (int.MaxValue, numero);
            }

            return (numericPart, numero[digitCount..]);
        }
    }
}
