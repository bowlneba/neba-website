namespace Neba.Api.Encryption;

/// <summary>
/// Wraps and unwraps the one-time AES key of an encrypted value with an RSA key.
/// </summary>
internal interface IKeyWrapper
{
    /// <summary>
    /// Wraps a key with the current key version.
    /// </summary>
    Task<WrappedKey> WrapAsync(byte[] key, CancellationToken ct);

    /// <summary>
    /// Unwraps a key with the named key version. Throws
    /// <see cref="System.Security.Cryptography.CryptographicException"/> if the version or wrapped key is bad.
    /// </summary>
    Task<byte[]> UnwrapAsync(string keyVersion, byte[] wrappedKey, CancellationToken ct);
}

internal sealed record WrappedKey(string KeyVersion, byte[] Value);
