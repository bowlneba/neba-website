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

            // Resolves BlobServiceClient (registered above) lazily from the real service provider
            // on first use, rather than building a temporary one — doing that previously disposed
            // the Azure client library's own registration cache, which is shared with (and
            // poisoned) every service provider built afterward, crashing the app on startup with
            // "Cannot access a disposed object: 'ClientRegistration'".
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

            return builder;
        }
    }
}