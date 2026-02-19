using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.Application.Clock;
using Neba.Infrastructure.BackgroundJobs;
using Neba.Infrastructure.Clock;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Documents;
using Neba.Infrastructure.Storage;
using Neba.Infrastructure.Telemetry.Tracing;

namespace Neba.Infrastructure;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable CA1708 // Identifiers should differ by more than case

/// <summary>
/// Extension methods to add infrastructure dependencies to the service collection.
/// </summary>
public static class InfrastructureConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Adds infrastructure dependencies to the service collection.
        /// </summary>
        /// <returns>The updated service collection.</returns>
        public WebApplicationBuilder AddInfrastructure()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddTracing();

            // caching decorators can go here

            builder
                .AddDatabase()
                .AddKeyVault()
                .AddStorage();

            builder.Services.AddBackgroundJobs(builder.Configuration);
            builder.Services.AddGoogleDrive(builder.Configuration);

            builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddSingleton<IStopwatchProvider, StopwatchProvider>();

            return builder;
        }

        private WebApplicationBuilder AddKeyVault()
        {
            var keyVaultConnectionString = builder.Configuration.GetConnectionString("keyvault");

            if (string.IsNullOrWhiteSpace(keyVaultConnectionString))
            {
                return builder;
            }

            builder.Configuration.AddAzureKeyVaultSecrets(keyVaultConnectionString);

            return builder;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Uses infrastructure middleware in the application.
        /// </summary>
        /// <returns>The updated web application.</returns>
        public WebApplication UseInfrastructure()
        {
            app.UseBackgroundJobsDashboard();
            app.UseDocumentSyncJobs();

            return app;
        }
    }
}