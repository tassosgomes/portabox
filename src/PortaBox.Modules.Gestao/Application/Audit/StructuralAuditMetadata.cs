namespace PortaBox.Modules.Gestao.Application.Audit;

/// <summary>
/// Centraliza os contratos de metadata JSONB para eventos estruturais de auditoria.
/// </summary>
public static class StructuralAuditMetadata
{
    /// <summary>
    /// Cria o metadata de <c>BlocoCriado</c>: <c>{ blocoId, nome }</c>.
    /// </summary>
    public static IDictionary<string, object> ForBlocoCriado(Guid blocoId, string nome)
    {
        return CreateBlocoMetadata(blocoId, nome);
    }

    /// <summary>
    /// Cria o metadata de <c>BlocoRenomeado</c>: <c>{ blocoId, nomeAntes, nomeDepois }</c>.
    /// </summary>
    public static IDictionary<string, object> ForBlocoRenomeado(Guid blocoId, string nomeAntes, string nomeDepois)
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["blocoId"] = blocoId,
            ["nomeAntes"] = nomeAntes,
            ["nomeDepois"] = nomeDepois
        };
    }

    /// <summary>
    /// Cria o metadata de <c>BlocoInativado</c>: <c>{ blocoId, nome }</c>.
    /// </summary>
    public static IDictionary<string, object> ForBlocoInativado(Guid blocoId, string nome)
    {
        return CreateBlocoMetadata(blocoId, nome);
    }

    /// <summary>
    /// Cria o metadata de <c>BlocoReativado</c>: <c>{ blocoId, nome }</c>.
    /// </summary>
    public static IDictionary<string, object> ForBlocoReativado(Guid blocoId, string nome)
    {
        return CreateBlocoMetadata(blocoId, nome);
    }

    /// <summary>
    /// Cria o metadata de <c>UnidadeCriada</c>: <c>{ unidadeId, blocoId, andar, numero }</c>.
    /// </summary>
    public static IDictionary<string, object> ForUnidadeCriada(Guid unidadeId, Guid blocoId, int andar, string numero)
    {
        return CreateUnidadeMetadata(unidadeId, blocoId, andar, numero);
    }

    /// <summary>
    /// Cria o metadata de <c>UnidadeInativada</c>: <c>{ unidadeId, blocoId, andar, numero }</c>.
    /// </summary>
    public static IDictionary<string, object> ForUnidadeInativada(Guid unidadeId, Guid blocoId, int andar, string numero)
    {
        return CreateUnidadeMetadata(unidadeId, blocoId, andar, numero);
    }

    /// <summary>
    /// Cria o metadata de <c>UnidadeReativada</c>: <c>{ unidadeId, blocoId, andar, numero }</c>.
    /// </summary>
    public static IDictionary<string, object> ForUnidadeReativada(Guid unidadeId, Guid blocoId, int andar, string numero)
    {
        return CreateUnidadeMetadata(unidadeId, blocoId, andar, numero);
    }

    private static IDictionary<string, object> CreateBlocoMetadata(Guid blocoId, string nome)
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["blocoId"] = blocoId,
            ["nome"] = nome
        };
    }

    private static IDictionary<string, object> CreateUnidadeMetadata(Guid unidadeId, Guid blocoId, int andar, string numero)
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["unidadeId"] = unidadeId,
            ["blocoId"] = blocoId,
            ["andar"] = andar,
            ["numero"] = numero
        };
    }
}
