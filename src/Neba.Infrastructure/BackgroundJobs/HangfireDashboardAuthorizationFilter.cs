using Hangfire.Dashboard;

namespace Neba.Infrastructure.BackgroundJobs;

internal sealed class HangfireApiDashboardAuthorizationFilter
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}