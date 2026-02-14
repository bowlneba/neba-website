using Asp.Versioning;

using FastEndpoints.AspVersioning;
using FastEndpoints.Swagger;

using Scalar.AspNetCore;

namespace Neba.Api.OpenApi;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should not be static

internal static class OpenApiConfiguration
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures OpenAPI documentation with Swagger and Scalar.
        /// </summary>
        public IServiceCollection AddOpenApiDocumentation()
        {
            services.SwaggerDocument(options =>
            {
                options.DocumentSettings = settings =>
                {
                    settings.DocumentName = "v1.0";
                    settings.Title = "NEBA API";
                    settings.Version = "v1.0";
                    settings.Description = "NEBA API Service";
                    settings.ApiVersion(new ApiVersion(1, 0));
                };

                options.AutoTagPathSegmentIndex = 0;
                options.EnableJWTBearerAuth = false;
                options.ShortSchemaNames = true;
                options.ExcludeNonFastEndpoints = true;

                options.TagDescriptions = tags =>
                {
                    tags["Weather"] = "Weather forecast endpoints";
                    tags["Tournaments"] = "Tournament management endpoints";
                    tags["Bowlers"] = "Bowler management endpoints";
                };
            });

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Configures OpenAPI documentation middleware with Scalar UI.
        /// </summary>
        public WebApplication UseOpenApiDocumentation()
        {
            app.UseOpenApi(config => config.Path = "/openapi/{documentName}.json");

            app.MapScalarApiReference(config => config
                .WithTitle("NEBA API")
                .WithTheme(ScalarTheme.DeepSpace)
                .AddDocument("v1.0"));

            return app;
        }
    }
}