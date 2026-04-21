namespace PortaBox.Modules.Gestao.Application.Estrutura;

/// <summary>
/// Arvore completa do condominio ordenada alfabeticamente por bloco,
/// numericamente por andar e semanticamente por numero da unidade.
/// </summary>
public sealed record EstruturaDto(
    Guid CondominioId,
    string NomeFantasia,
    IReadOnlyList<BlocoNodeDto> Blocos,
    DateTime GeradoEm);

public sealed record BlocoNodeDto(
    Guid Id,
    string Nome,
    bool Ativo,
    IReadOnlyList<AndarNodeDto> Andares);

public sealed record AndarNodeDto(
    int Andar,
    IReadOnlyList<UnidadeLeafDto> Unidades);

public sealed record UnidadeLeafDto(
    Guid Id,
    string Numero,
    bool Ativo);
