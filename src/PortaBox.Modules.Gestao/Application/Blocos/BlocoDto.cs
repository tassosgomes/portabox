namespace PortaBox.Modules.Gestao.Application.Blocos;

public sealed record BlocoDto(
    Guid Id,
    Guid CondominioId,
    string Nome,
    bool Ativo,
    DateTime? InativadoEm);
