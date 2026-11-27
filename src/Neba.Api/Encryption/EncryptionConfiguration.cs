using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Neba.Api.Encryption;

internal static class EncryptionConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddEncryption()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IValidateOptions<EncryptionKeySettings>, EncryptionKeySettingsValidator>());

            builder.AddEncryptionKey<BowlerPii>();

            return builder;
        }

        private void AddEncryptionKey<TKey>() where TKey : IEncryptionKey
        {
            builder.Services
                .AddOptions<EncryptionKeySettings>(TKey.Name)
                .Bind(builder.Configuration.GetSection($"Encryption:Keys:{TKey.Name}"))
                .ValidateOnStart();

            builder.Services.AddSingleton<IEncryptor<TKey>>(sp => new Encryptor<TKey>(
                CreateKeyWrapper(sp.GetRequiredService<IOptionsMonitor<EncryptionKeySettings>>().Get(TKey.Name)),
                sp.GetRequiredService<ILogger<Encryptor<TKey>>>()));

            builder.Services.AddSingleton<IHasher<TKey>>(sp => new Hasher<TKey>(Convert.FromBase64String(
                sp.GetRequiredService<IOptionsMonitor<EncryptionKeySettings>>().Get(TKey.Name).HashKey)));
        }
    }

    // The validator has already rejected a local key outside Development and a missing Key Vault URI
    // there, so a local key is used only in Development with no URI set.
    private static IKeyWrapper CreateKeyWrapper(EncryptionKeySettings settings)
    {
        if (!KeyVaultKeyId.TryParse(settings.KeyVaultKeyUri, out var keyId))
        {
            return new LocalKeyWrapper(settings.LocalPrivateKey);
        }

        var credential = new DefaultAzureCredential();

        return new KeyVaultKeyWrapper(keyId, uri => new CryptographyClient(uri, credential));
    }
}
