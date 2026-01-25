using Hangfire.Dashboard;

namespace Neba.Website.Server.BackgroundJobs;

internal sealed class HangfireUiDashboardAuthorizationFilter
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In this example, we allow all users to see the Hangfire Dashboard.
        // Implement your own authorization logic here.
        return true;
    }
}