using System.Runtime.CompilerServices;

using Audit.Core;
using Audit.EntityFramework;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Neba.Api.Database.Interceptors;

// Audit.EntityFramework's own AuditSaveChangesInterceptor keeps its per-save state
// (IAuditDbContext/IAuditScope) in plain instance fields. AppDbContext is registered via
// AddDbContextPool, which builds one shared DbContextOptions - and therefore reuses one
// shared interceptor instance - for the whole pool. Two SaveChangesAsync calls on different
// pooled DbContext instances that overlap in time (any concurrent request/job traffic) then
// stomp on each other's fields, and whichever save finishes second reads a scope that was
// overwritten by the other save - the resulting "EF:AppDbContext" audit event is silently
// dropped rather than failing loudly.
//
// This interceptor performs the identical work via Audit.EntityFramework's own public
// DbContextHelper, but keys per-save state by DbContext instance instead of by interceptor
// instance. That is safe to share across the pool: a single DbContext instance is never used
// concurrently by more than one save at a time - only different pooled instances run
// concurrently - so per-instance keyed state can't be raced the way shared fields were.
internal sealed class PooledAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly DbContextHelper _helper = new();
    private readonly ConditionalWeakTable<DbContext, ScopeState> _scopes = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            var auditContext = new DefaultAuditContext(context);
            _helper.SetConfig(auditContext);
            var scope = _helper.BeginSaveChanges(auditContext);
            _scopes.AddOrUpdate(context, new ScopeState(auditContext, scope));
        }

        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            var auditContext = new DefaultAuditContext(context);
            _helper.SetConfig(auditContext);
            var scope = await _helper.BeginSaveChangesAsync(auditContext, cancellationToken);
            _scopes.AddOrUpdate(context, new ScopeState(auditContext, scope));
        }

        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is { } context && _scopes.TryGetValue(context, out var state))
        {
            _helper.EndSaveChanges(state.AuditContext, state.Scope, result);
            _scopes.Remove(context);
        }

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && _scopes.TryGetValue(context, out var state))
        {
            await _helper.EndSaveChangesAsync(state.AuditContext, state.Scope, result, cancellationToken: cancellationToken);
            _scopes.Remove(context);
        }

        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is { } context && _scopes.TryGetValue(context, out var state))
        {
            _helper.EndSaveChanges(state.AuditContext, state.Scope, 0, eventData.Exception);
            _scopes.Remove(context);
        }
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && _scopes.TryGetValue(context, out var state))
        {
            await _helper.EndSaveChangesAsync(state.AuditContext, state.Scope, 0, eventData.Exception, cancellationToken);
            _scopes.Remove(context);
        }
    }

    private sealed record ScopeState(IAuditDbContext AuditContext, IAuditScope Scope);
}