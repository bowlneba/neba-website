using System.Reflection;

using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Runs <see cref="AutomaticRetryAttribute"/> for every job except those whose argument type carries
/// <see cref="NoAutomaticRetryAttribute"/>.
/// </summary>
/// <remarks>
/// Keeps the inner filter's <see cref="JobFilterAttribute.Order"/>, so a filter registered after it
/// (<see cref="DiscordJobFailureFilter"/>) still sees only final failures. Hangfire's own default
/// <see cref="AutomaticRetryAttribute"/> must be removed from <see cref="GlobalJobFilters.Filters"/>
/// when this filter is registered, otherwise it would still retry opted-out jobs.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class OptOutAutomaticRetryAttribute
    : JobFilterAttribute, IElectStateFilter, IApplyStateFilter
{
    public OptOutAutomaticRetryAttribute(AutomaticRetryAttribute inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        Inner = inner;
        Order = inner.Order;
    }

    /// <summary>
    /// The retry filter that runs for every job that has not opted out.
    /// </summary>
    public AutomaticRetryAttribute Inner { get; }

    public void OnStateElection(ElectStateContext context)
    {
        if (OptsOut(context.BackgroundJob.Job))
        {
            return;
        }

        Inner.OnStateElection(context);
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        => Inner.OnStateApplied(context, transaction);

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        => Inner.OnStateUnapplied(context, transaction);

    private static bool OptsOut(Job? job)
        => job is not null
            && job.Args.Any(arg => arg?.GetType().GetCustomAttribute<NoAutomaticRetryAttribute>() is not null);
}