using System.Collections.Concurrent;
using System.Security.Cryptography;

using Azure;
using Azure.Security.KeyVault.Keys.Cryptography;

namespace Neba.Api.Encryption;

/// <summary>
/// Wraps keys with an RSA key in Key Vault. The configured URI names the version that wraps;
/// each value names the version that unwraps it, so old versions must stay enabled.
/// </summary>
internal sealed class KeyVaultKeyWrapper(KeyVaultKeyId keyId, Func<Uri, CryptographyClient> createClient)
    : IKeyWrapper
{
    private readonly ConcurrentDictionary<string, CryptographyClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    public async Task<WrappedKey> WrapAsync(byte[] key, CancellationToken ct)
    {
        var client = _clients.GetOrAdd(keyId.Version, version => createClient(keyId.ForVersion(version)));
        var result = await client.WrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, key, ct);

        return new WrappedKey(keyId.Version, result.EncryptedKey);
    }

    public async Task<byte[]> UnwrapAsync(string keyVersion, byte[] wrappedKey, CancellationToken ct)
    {
        if (!KeyVaultKeyId.IsValidVersion(keyVersion))
        {
            throw new CryptographicException("Unknown key version.");
        }

        if (!_clients.TryGetValue(keyVersion, out var client))
        {
            client = createClient(keyId.ForVersion(keyVersion));
        }

        try
        {
            var result = await client.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, wrappedKey, ct);

            // Cache only versions that worked, so made-up versions in client input cannot fill memory.
            _clients.TryAdd(keyVersion, client);

            return result.Key;
        }
        catch (RequestFailedException ex) when (ex.Status is 400 or 404)
        {
            // Bad wrapped key or missing version. Other failures (outage, access) are not bad input.
            throw new CryptographicException("Key Vault could not unwrap the key.", ex);
        }
    }
}
