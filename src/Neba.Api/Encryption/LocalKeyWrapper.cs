using System.Security.Cryptography;

namespace Neba.Api.Encryption;

/// <summary>
/// Wraps keys with an RSA key held in configuration. Development only.
/// </summary>
internal sealed class LocalKeyWrapper(string privateKeyBase64) : IKeyWrapper
{
    public const string KeyVersion = "local";

    private readonly byte[] _privateKey = Convert.FromBase64String(privateKeyBase64);

    public Task<WrappedKey> WrapAsync(byte[] key, CancellationToken ct)
    {
        using var rsa = CreateRsa();

        return Task.FromResult(new WrappedKey(KeyVersion, rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256)));
    }

    public Task<byte[]> UnwrapAsync(string keyVersion, byte[] wrappedKey, CancellationToken ct)
    {
        if (keyVersion != KeyVersion)
        {
            throw new CryptographicException("Unknown key version.");
        }

        using var rsa = CreateRsa();

        return Task.FromResult(rsa.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256));
    }

    // One instance per call: RSA instance members are not documented as thread-safe.
    private RSA CreateRsa()
    {
        var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(_privateKey, out _);

        return rsa;
    }
}
