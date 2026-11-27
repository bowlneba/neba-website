using System.Security.Cryptography;
using System.Text;

using ErrorOr;

namespace Neba.Api.Encryption;

internal sealed class Encryptor<TKey>(IKeyWrapper keyWrapper, ILogger<Encryptor<TKey>> logger)
    : IEncryptor<TKey> where TKey : IEncryptionKey
{
    public async Task<Encrypted<TKey>> EncryptAsync(string plaintext, CancellationToken ct)
        => new(await SealAsync(plaintext, EncryptionPurpose.Storage, ct));

    public Task<string> DecryptAsync(Encrypted<TKey> value, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(value);

        return OpenAsync(value.Value, EncryptionPurpose.Storage, ct);
    }

    public async Task<ErrorOr<string>> DecryptFromClientAsync(string wireValue, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(wireValue))
        {
            return EncryptionErrors.InvalidClientValue;
        }

        try
        {
            return await OpenAsync(wireValue, EncryptionPurpose.Wire, ct);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            logger.LogClientValueRejected(ex, TKey.Name);

            return EncryptionErrors.InvalidClientValue;
        }
    }

    // Internal so tests can make wire values. Production code only seals for storage; a browser
    // seals wire values (field-encryption.js).
    internal async Task<string> SealAsync(string plaintext, EncryptionPurpose purpose, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var key = RandomNumberGenerator.GetBytes(Envelope.AesKeySize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        try
        {
            // Wrap first: the key version it returns is part of the additional data.
            var wrapped = await keyWrapper.WrapAsync(key, ct);
            var nonce = RandomNumberGenerator.GetBytes(Envelope.NonceSize);
            var tag = new byte[Envelope.TagSize];
            var ciphertext = new byte[plaintextBytes.Length];

            using var aes = new AesGcm(key, Envelope.TagSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag,
                Envelope.AdditionalData(TKey.Name, purpose, wrapped.KeyVersion));

            return new Envelope(wrapped.KeyVersion, wrapped.Value, nonce, tag, ciphertext).Encode();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    private async Task<string> OpenAsync(string value, EncryptionPurpose purpose, CancellationToken ct)
    {
        var envelope = Envelope.Decode(value);
        var key = await keyWrapper.UnwrapAsync(envelope.KeyVersion, envelope.WrappedKey, ct);
        var plaintextBytes = new byte[envelope.Ciphertext.Length];

        try
        {
            // A browser could wrap a shorter AES key; the format allows AES-256 only.
            if (key.Length != Envelope.AesKeySize)
            {
                throw new CryptographicException("The wrapped key has the wrong size.");
            }

            using var aes = new AesGcm(key, Envelope.TagSize);
            aes.Decrypt(envelope.Nonce, envelope.Ciphertext, envelope.Tag, plaintextBytes,
                Envelope.AdditionalData(TKey.Name, purpose, envelope.KeyVersion));

            return Encoding.UTF8.GetString(plaintextBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }
}

internal static partial class EncryptorLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected a client-encrypted value for key {KeyName}")]
    public static partial void LogClientValueRejected(this ILogger logger, Exception exception, string keyName);
}
