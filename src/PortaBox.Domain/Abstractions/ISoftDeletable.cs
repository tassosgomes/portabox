namespace PortaBox.Domain.Abstractions;

/// <summary>
/// Marker interface for entities that use the standard soft-delete lifecycle.
/// </summary>
public interface ISoftDeletable
{
    bool Ativo { get; }

    DateTime? InativadoEm { get; }

    Guid? InativadoPor { get; }
}
