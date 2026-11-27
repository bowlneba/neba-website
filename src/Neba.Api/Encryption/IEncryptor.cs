using ErrorOr;

namespace Neba.Api.Encryption;

/// <summary>
/// Encrypts and decrypts values with <typeparamref name="TKey"/>.
/// </summary>
internal interface IEncryptor<TKey> where TKey : IEncryptionKey
{
    /// <summary>
    /// Encrypts a value for storage.
    /// </summary>
    Task<Encrypted<TKey>> EncryptAsync(string plaintext, CancellationToken ct);

    /// <summary>
    /// Decrypts a stored value. Throws if the value is bad.
    /// </summary>
    Task<string> DecryptAsync(Encrypted<TKey> value, CancellationToken ct);

    /// <summary>
    /// Decrypts a value a browser encrypted. Treat the result as untrusted input.
    /// </summary>
    Task<ErrorOr<string>> DecryptFromClientAsync(string wireValue, CancellationToken ct);
}
