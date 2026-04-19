using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Modules.Gestao.Application.Common;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Modules.Gestao.Application.Queries.ListCondominios;

public sealed record ListCondominiosQuery : IQuery<PagedResult<CondominioListItemDto>>
{
    public ListCondominiosQuery(int page = 1, int pageSize = 20, CondominioStatus? status = null, string? searchTerm = null)
    {
        Page = page < 1 ? 1 : page;
        PageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);
        Status = status;
        SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
    }

    public int Page { get; }

    public int PageSize { get; }

    public CondominioStatus? Status { get; }

    public string? SearchTerm { get; }
}
