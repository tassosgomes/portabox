using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Estrutura;

/// <summary>
/// Retorna a arvore completa do condominio ordenada alfabeticamente por bloco,
/// numericamente por andar e semanticamente por numero da unidade.
/// </summary>
public sealed record GetEstruturaQuery(Guid CondominioId, bool IncludeInactive) : IQuery<EstruturaDto>;
