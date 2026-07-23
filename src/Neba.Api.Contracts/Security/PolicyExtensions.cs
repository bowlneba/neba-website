using Microsoft.AspNetCore.Authorization;

namespace Neba.Api.Contracts.Security;

/// <summary>
/// Extension methods for <see cref="AuthorizationBuilder"/> to add Neba-specific policies.
/// </summary>
public static class PolicyExtensions
{
    extension(AuthorizationBuilder builder)
    {
        /// <summary>
        /// Adds Neba-specific authorization policies to the <see cref="AuthorizationBuilder"/>.
        /// </summary>
        /// <returns></returns>
        public AuthorizationBuilder AddNebaPolicies()
        {
            builder.AddPolicy(Permissions.CanManageArticlesPolicyName, policy => policy
                .RequireAssertion(context => context.User.HasAnyPermission(Permissions.ArticleManagementPermissions)));

            builder.AddPolicy(Permissions.CanManageSponsorsPolicyName, policy => policy
                .RequireAssertion(context => context.User.HasAnyPermission(Permissions.SponsorManagementPermissions)));

            builder.AddPolicy(Permissions.CanManageTournamentsPolicyName, policy => policy
                .RequireAssertion(context => context.User.HasAnyPermission(Permissions.TournamentManagementPermissions)));

            return builder;
        }
    }
}