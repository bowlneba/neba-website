using Hangfire.Dashboard;

namespace Neba.Api.BackgroundJobs;

internal sealed class HangfireApiDashboardAuthorizationFilter
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}