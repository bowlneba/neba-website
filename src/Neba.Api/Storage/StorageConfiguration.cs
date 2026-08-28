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
            // BlobServiceClient was just registered above by AddAzureBlobServiceClient; resolve it
            // from a short-lived provider since AddDataProtection() needs a concrete BlobClient at
            // configuration time, before the real service provider is built.
            using var tempProvider = builder.Services.BuildServiceProvider();
            var blobServiceClient = tempProvider.GetRequiredService<BlobServiceClient>();

            var containerClient = blobServiceClient.GetBlobContainerClient(DataProtectionContainerName);
            containerClient.CreateIfNotExists();

            builder.Services
                .AddDataProtection()
                .SetApplicationName(DataProtectionApplicationName)
                .PersistKeysToAzureBlobStorage(containerClient.GetBlobClient(DataProtectionKeysBlobName));
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