using Hangfire.Dashboard;

namespace Neba.Infrastructure.BackgroundJobs;

internal sealed class HangfireDashboardAuthorizationFilter
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}