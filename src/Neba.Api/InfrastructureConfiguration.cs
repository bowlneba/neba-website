
using Neba.Api.BackgroundJobs;
using Neba.Api.Caching;
using Neba.Api.Clock;
using Neba.Api.Database;
using Neba.Api.Documents;
using Neba.Api.Storage;
using Neba.Api.Telemetry.Tracing;

namespace Neba.Api;

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
        /// <returns>
        /// The updated service collection.
        /// </returns>
        public WebApplicationBuilder AddInfrastructure()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddTracing();

            builder.Services.DecorateCachedQueryHandlers();

            builder
                .AddDatabase()
                .AddKeyVault()
                .AddStorage();

            builder.Services.AddCaching(builder.Configuration);
            builder.Services.AddBackgroundJobs(builder.Configuration);
            builder.Services.AddGoogleDrive(builder.Configuration);

            builder.Services.AddSingleton(TimeProvider.System);
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
        /// <returns>
        /// The updated web application.
        /// </returns>
        public WebApplication UseInfrastructure()
        {
            app.UseBackgroundJobsDashboard();
            app.UseDocumentSyncJobs();

            return app;
        }
    }
}