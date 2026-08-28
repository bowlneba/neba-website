using Azure.Storage.Blobs;

using Microsoft.AspNetCore.DataProtection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Uploads;

namespace Neba.Api.Storage;

internal static class StorageConfiguration
{
    private const string CleanupOrphanedUploadsRecurringJobId = "cleanup-orphaned-uploads";

    // Shared with Neba.Website.Server's AddAccountServices — same container/blob name and
    // application name so a cookie-auth ticket issued by the website can be decrypted here,
    // letting a signed-in Webmaster navigate straight to /background-jobs without a separate
    // token. Keep both in sync if either changes.
    internal const string DataProtectionApplicationName = "Neba";
    private const string DataProtectionContainerName = "dataprotection-keys";
    private const string DataProtectionKeysBlobName = "keys.xml";

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddStorage()
        {
            builder.AddAzureBlobServiceClient("blob");

            builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();
            builder.Services.AddScoped<IUploadStagingService, UploadStagingService>();

            builder.AddSharedDataProtection();

            return builder;
        }

        private void AddSharedDataProtection()
        {
            // Resolves BlobServiceClient (registered above by AddAzureBlobServiceClient) lazily
            // from the real service provider on first use, rather than building a temporary one —
            // doing that previously disposed the Azure client library's own registration cache,
            // which is shared with (and poisoned) every service provider built afterward, crashing
            // the app on startup with "Cannot access a disposed object: 'ClientRegistration'".
            builder.Services
                .AddDataProtection()
                .SetApplicationName(DataProtectionApplicationName)
                .PersistKeysToAzureBlobStorage(sp =>
                {
                    var containerClient = sp.GetRequiredService<BlobServiceClient>()
                        .GetBlobContainerClient(DataProtectionContainerName);
                    containerClient.CreateIfNotExists();

                    return containerClient.GetBlobClient(DataProtectionKeysBlobName);
                });
        }
    }

    extension(WebApplication app)
    {
        public void UseUploadCleanupJob()
        {
            using var scope = app.Services.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();

            scheduler.AddOrUpdateRecurring(
                CleanupOrphanedUploadsRecurringJobId,
                new CleanupOrphanedUploadsJob(),
                "0 5 */2 * *"); // Every other day at 5:00 AM
        }
    }
}