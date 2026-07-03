using Microsoft.FeatureManagement;

using Neba.Api.Contracts.FeatureManagement;

namespace Neba.Api.FeatureManagement;

internal static class FeatureManagementConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddFeatureManagement()
        {
            builder.Services
                .AddFeatureManagement(builder.Configuration.GetSection("FeatureManagement"))
                .AddFeatureFilter<AllowedEmailFilter>();

            return builder;
        }
    }
}