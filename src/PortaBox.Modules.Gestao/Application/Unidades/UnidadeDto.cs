namespace PortaBox.Modules.Gestao.Application.Unidades;

public sealed record UnidadeDto(
    Guid Id,
    Guid BlocoId,
    int Andar,
    string Numero,
    bool Ativo,
    DateTime? InativadoEm);
