using PortaBox.Application.Abstractions.MultiTenancy;

namespace PortaBox.Infrastructure.MultiTenancy;

/// <summary>
/// Scoped implementation of <see cref="ITenantContext" />.
/// </summary>
/// <remarks>
/// Uses <see cref="AsyncLocal{T}" /> so the active tenant flows correctly across async boundaries
/// and nested scopes, including background/outbox work that temporarily overrides the tenant inside
/// a broader request or operation.
///
/// Lifetime: Scoped — one instance per HTTP request; the middleware populates it.
/// </remarks>
public sealed class TenantContext : ITenantContext
{
    private readonly AsyncLocal<TenantScopeState?> _currentScope = new();

    /// <inheritdoc />
    public Guid? TenantId => _currentScope.Value?.TenantId;

    /// <inheritdoc />
    public IDisposable BeginScope(Guid tenantId)
    {
        var previous = _currentScope.Value;
        var current = new TenantScopeState(tenantId, previous);
        _currentScope.Value = current;

        return new TenantScope(this, current);
    }

    private void Restore(TenantScopeState? current)
    {
        if (ReferenceEquals(_currentScope.Value, current))
        {
            _currentScope.Value = current?.Previous;
        }
    }

    private sealed class TenantScope(TenantContext context, TenantScopeState current) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            context.Restore(current);
        }
    }

    private sealed class TenantScopeState(Guid tenantId, TenantScopeState? previous)
    {
        public Guid TenantId { get; } = tenantId;

        public TenantScopeState? Previous { get; } = previous;
    }
}
