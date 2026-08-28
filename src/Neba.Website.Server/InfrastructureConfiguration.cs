using Azure.Storage.Blobs;

using Microsoft.AspNetCore.DataProtection;

namespace Neba.Website.Server;

#pragma warning disable CA1708 // Identifiers should differ by more than case

/// <summary>
/// Extension methods to add infrastructure dependencies to the web app builder.
/// </summary>
internal static class InfrastructureConfiguration
{
    // Shared with Neba.Api's Storage.StorageConfiguration — same application name (and, via
    // AddAccountServices' SharedAuthCookieName, the same cookie name) so a cookie-auth ticket
    // this app encrypts can be decrypted by the API. Keep both in sync if either changes.
    private const string DataProtectionApplicationName = "Neba";
    private const string DataProtectionContainerName = "dataprotection-keys";
    private const string DataProtectionKeysBlobName = "keys.xml";

    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Adds infrastructure dependencies to the service collection.
        /// </summary>
        /// <returns>
        /// The updated builder.
        /// </returns>
        public WebApplicationBuilder AddInfrastructure()
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.AddKeyVault().AddSharedDataProtection();
        }

        private WebApplicationBuilder AddKeyVault()
        {
            var keyVaultConnectionString = builder.Configuration.GetConnectionString("keyvault");

            if (string.IsNullOrWhiteSpace(keyVaultConnectionString))
            {
                return builder;
            }

            builder.Configuration.AddAzureKeyVaultSecrets("keyvault");

            return builder;
        }

        private WebApplicationBuilder AddSharedDataProtection()
        {
            var blobConnectionString = builder.Configuration.GetConnectionString("blob");

            if (string.IsNullOrWhiteSpace(blobConnectionString))
            {
                return builder;
            }

            builder.AddAzureBlobServiceClient("blob");

            // BlobServiceClient was just registered above; resolve it from a short-lived provider
            // since AddDataProtection() needs a concrete BlobClient at configuration time, before
            // the real service provider is built.
            using var tempProvider = builder.Services.BuildServiceProvider();
            var blobServiceClient = tempProvider.GetRequiredService<BlobServiceClient>();

            var containerClient = blobServiceClient.GetBlobContainerClient(DataProtectionContainerName);
            containerClient.CreateIfNotExists();

            builder.Services
                .AddDataProtection()
                .SetApplicationName(DataProtectionApplicationName)
                .PersistKeysToAzureBlobStorage(containerClient.GetBlobClient(DataProtectionKeysBlobName));

            return builder;
        }
    }
}