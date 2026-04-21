using PortaBox.Domain.Abstractions;

namespace PortaBox.Domain;

public abstract class SoftDeleteableAggregateRoot : AggregateRoot, ISoftDeletable
{
    public bool Ativo { get; protected set; } = true;

    public DateTime? InativadoEm { get; protected set; }

    public Guid? InativadoPor { get; protected set; }

    protected Result Inativar(Guid porUserId, DateTime agoraUtc)
    {
        if (!Ativo)
        {
            return Result.Failure("A entidade ja esta inativa.");
        }

        Ativo = false;
        InativadoEm = agoraUtc;
        InativadoPor = porUserId;

        return Result.Success();
    }

    protected Result Reativar(Guid porUserId, DateTime agoraUtc)
    {
        if (Ativo)
        {
            return Result.Failure("A entidade ja esta ativa.");
        }

        Ativo = true;
        InativadoEm = null;
        InativadoPor = null;

        return Result.Success();
    }
}
