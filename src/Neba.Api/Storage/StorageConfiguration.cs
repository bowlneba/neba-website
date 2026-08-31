using Azure.Identity;
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
    private const string DataProtectionKeyVaultKeyUriConfigKey = "DataProtection:KeyVaultKeyUri";

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
            var dataProtectionBuilder = builder.Services
                .AddDataProtection()
                .SetApplicationName(DataProtectionApplicationName)
                .PersistKeysToAzureBlobStorage(sp =>
                {
                    var containerClient = sp.GetRequiredService<BlobServiceClient>()
                        .GetBlobContainerClient(DataProtectionContainerName);
                    containerClient.CreateIfNotExists();

                    return containerClient.GetBlobClient(DataProtectionKeysBlobName);
                });

            // Encrypts the key ring at rest instead of persisting it as plaintext XML - without
            // this, anyone who can read the blob (an over-broad role grant, a leaked SAS token, a
            // compromised storage account key) could mint valid auth cookies for any identity.
            // No Key Vault key is provisioned for local dev (see AppHost.cs), so this is a no-op
            // there and the key ring stays unencrypted against the local storage emulator.
            var keyVaultKeyUri = builder.Configuration[DataProtectionKeyVaultKeyUriConfigKey];
            if (!string.IsNullOrWhiteSpace(keyVaultKeyUri))
            {
                dataProtectionBuilder.ProtectKeysWithAzureKeyVault(new Uri(keyVaultKeyUri), new DefaultAzureCredential());
            }
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