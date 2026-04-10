using Asp.Versioning;

using FastEndpoints.AspVersioning;

namespace Neba.Api.Versioning;

internal static class VersioningConfiguration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures header-based API versioning using the X-Api-Version header.
        /// Defaults to version 1.0 when no version is specified.
        /// </summary>
        public IServiceCollection AddVersioning()
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