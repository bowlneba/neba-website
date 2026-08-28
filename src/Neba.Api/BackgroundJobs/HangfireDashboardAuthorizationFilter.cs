using System.Security.Claims;

using Hangfire.Dashboard;

using Neba.Api.Contracts.Security;

namespace Neba.Api.BackgroundJobs;

internal sealed class HangfireApiDashboardAuthorizationFilter
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
        => Authorize(context.GetHttpContext().User);

    /// <summary>
    /// Split out from <see cref="Authorize(DashboardContext)"/> so authorization logic can be unit
    /// tested directly against a <see cref="ClaimsPrincipal"/> — Hangfire's <see cref="DashboardContext"/>
    /// has no parameterless constructor and cannot be constructed or mocked in a unit test.
    /// </summary>
    internal static bool Authorize(ClaimsPrincipal user)
        => user.HasAnyPermission([Permissions.ViewBackgroundJobsDashboard]);
}