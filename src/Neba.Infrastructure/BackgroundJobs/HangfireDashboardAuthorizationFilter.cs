using Hangfire.Dashboard;

namespace Neba.Infrastructure.BackgroundJobs;

internal sealed class HangfireJobDashboardAuthorizationFilter
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}