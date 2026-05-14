using Microsoft.Extensions.Options;

using Neba.Api.BackgroundJobs;
using Neba.Application.Documents;
using Neba.Application.Documents.SyncDocument;

namespace Neba.Api.Documents;

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