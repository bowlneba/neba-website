using Asp.Versioning;

using FastEndpoints.AspVersioning;

using Microsoft.Extensions.DependencyInjection;

namespace Neba.Api.Versioning;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should not be static

internal static class VersioningConfiguration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures header-based API versioning using the X-Api-Version header.
        /// Defaults to version 1.0 when no version is specified.
        /// </summary>
        public IServiceCollection AddApiVersioning()
        {
            services.AddVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new HeaderApiVersionReader("x-api-version");
            });

            return services;
        }
    }
}
