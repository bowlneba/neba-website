using System.Security.Cryptography;

using Neba.Api.Encryption;

namespace Neba.Api.Tests.Encryption;

/// <summary>A second key marker. Production has no second key.</summary>
internal sealed class OtherPii : IEncryptionKey
{
    public static string Name => "OtherPii";
}

/// <summary>Several key versions, so a test can wrap with a newer one and still unwrap an older one.</summary>
internal sealed class VersionedKeyWrapper : IKeyWrapper, IDisposable
{
    private readonly Dictionary<string, RSA> _keys = [];

    public VersionedKeyWrapper(string version = "v1") => Rotate(version);

    public string CurrentVersion { get; private set; } = string.Empty;

    public void Rotate(string version)
    {
        _keys[version] = RSA.Create(2048);
        CurrentVersion = version;
    }

    public Task<WrappedKey> WrapAsync(byte[] key, CancellationToken ct)
        => Task.FromResult(new WrappedKey(CurrentVersion, _keys[CurrentVersion].Encrypt(key, RSAEncryptionPadding.OaepSHA256)));

    public Task<byte[]> UnwrapAsync(string keyVersion, byte[] wrappedKey, CancellationToken ct)
        => _keys.TryGetValue(keyVersion, out var rsa)
            ? Task.FromResult(rsa.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256))
            : throw new CryptographicException("Unknown key version.");

    public void Dispose()
    {
        foreach (var rsa in _keys.Values)
        {
            rsa.Dispose();
        }
    }
}
