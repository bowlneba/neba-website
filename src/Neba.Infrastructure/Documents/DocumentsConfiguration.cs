using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Application;
using Neba.Application.BackgroundJobs;
using Neba.Application.Documents;
using Neba.Application.Documents.SyncDocument;

namespace Neba.Infrastructure.Documents;

#pragma warning disable S1144 // Unused private methods are used in other implementations of IDocumentsService and may be used in the future as features are added. These methods are not currently used but are intentionally included for future extensibility.
#pragma warning disable S2325 // Methods that do not access instance data are made static. These methods are not currently used but are intentionally included for future extensibility and may require instance data in the future as features are added.
internal static class DocumentsConfiguration
{
    extension(IServiceCollection services)
    {
        public void AddGoogleDrive(IConfiguration config)
        {
            // Configure and validate GoogleDriveSettings from appsettings.json
            services.AddOptions<GoogleSettings>()
                .Bind(config.GetSection(GoogleSettings.ConfigurationSectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register settings singleton with private key preprocessing
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GoogleSettings>>();
                var settings = options.Value;

                var processedCredentials = settings.Credentials with
                {
                    PrivateKey = settings.Credentials.PrivateKey.Replace("\\n", "\n", StringComparison.Ordinal)
                };

                return settings with { Credentials = processedCredentials };
            });

            // Register HTML processor
            services.AddSingleton<HtmlProcessor>();

            // Register Google Drive service as IDocumentsService
            services.AddSingleton<IDocumentsService, GoogleDriveService>();
        }
    }

    extension(WebApplication app)
    {
        public void UseDocumentSyncJobs()
        {
            using var scope = app.Services.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();
            var settings = scope.ServiceProvider.GetRequiredService<GoogleSettings>();
            
            foreach (var documentName in settings.Documents.Select(document => document.Name))
            {
                scheduler.AddOrUpdateRecurring(
                    $"sync-document-{documentName}",
                    new SyncDocumentToStorageJob
                    {
                        DocumentName = documentName,
                        TriggeredBy = "scheduled"
                    },
                    "0 5 1-7 * 1"); // First Monday at 5:00 AM
            }
        }
    }
}