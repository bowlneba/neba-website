
using Neba.Api.Auditing;
using Neba.Api.BackgroundJobs;
using Neba.Api.Caching;
using Neba.Api.Clock;
using Neba.Api.Contracts.Compliance;
using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Documents;
using Neba.Api.Email;
using Neba.Api.Encryption;
using Neba.Api.Features.News;
using Neba.Api.Features.Seasons;
using Neba.Api.Features.Series;
using Neba.Api.Features.Tournaments;
using Neba.Api.Legacy;
using Neba.Api.RateLimiting;
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
            builder.Services.AddRateLimiting(builder.Configuration);

            builder.Services.DecorateCachedQueryHandlers();

            builder
                .AddAuditing()
                .AddDatabase()
                .AddKeyVault()
                .AddEncryption()
                .AddStorage()
                .AddEmail()
                .AddDiscord()
                .AddRedaction();

            builder.Services.AddCaching();
            builder.Services.AddBackgroundJobs(builder.Configuration);
            builder.Services.AddGoogleDrive(builder.Configuration);
            builder.Services.AddNews(builder.Configuration);
            builder.Services.AddTournaments(builder.Configuration);
            builder.Services.AddSeries();

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<IStopwatchProvider, StopwatchProvider>();

            return builder;
        }

        private WebApplicationBuilder AddKeyVault()
            => builder.AddKeyVault(name => builder.Configuration.AddAzureKeyVaultSecrets(name));

        // The loader is a parameter so tests can see which vaults load without calling Azure.
        internal WebApplicationBuilder AddKeyVault(Action<string> addSecrets)
        {
            // "piivault" is a second vault that only the API references, for encryption keys and
            // their secrets.
            string[] vaults = ["keyvault", "piivault"];

            foreach (var connectionName in vaults.Where(
                name => !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(name))))
            {
                addSecrets(connectionName);
            }

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
            app.UseUploadCleanupJob();
            app.UseSeasonRolloverJob();
            app.UseSeriesRemoveMakeOfficialJob();
            app.UseLegacyWeeklySync();
            app.UseApiAuditMiddleware();

            return app;
        }
    }
}