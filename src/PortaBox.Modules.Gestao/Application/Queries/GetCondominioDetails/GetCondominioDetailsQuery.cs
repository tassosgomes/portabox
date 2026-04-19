using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;

public sealed record GetCondominioDetailsQuery(Guid CondominioId) : IQuery<CondominioDetailsDto>;
