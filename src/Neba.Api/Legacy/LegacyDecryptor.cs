using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

namespace Neba.Api.Legacy;

/// <summary>
/// Decrypts values the Software encrypted (Common/NEBA.Common/Encryption.cs in nebamgmt-v3).
/// Base64 of a 16-byte salt, a 16-byte IV and the ciphertext; AES-128-CBC with PKCS7 padding; the
/// key from PBKDF2 (SHA-1, 1,000 iterations).
/// </summary>
/// <remarks>
/// The scheme and its passphrase are weak, so treat legacy values and their backups as barely
/// protected. Nothing here authenticates the value: a wrong passphrase usually throws but can
/// return garbage, so check the result before using it.
/// </remarks>
internal sealed class LegacyDecryptor(IOptions<LegacySettings> settings)
{
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int KeySize = 16;
    private const int Iterations = 1000;

    /// <summary>
    /// Decrypts a legacy value. Empty input gives empty output, as the old code did.
    /// </summary>
    /// <exception cref="FormatException">The value is not Base64.</exception>
    /// <exception cref="CryptographicException">The value is too short or does not decrypt.</exception>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        var bytes = Convert.FromBase64String(cipherText);
        if (bytes.Length <= SaltSize + IvSize)
        {
            throw new CryptographicException("The legacy value is too short.");
        }

        // The old code's settings: they must match to read what it wrote, so weak values stay.
#pragma warning disable CA5379, CA5387, S4790, S5344 // Weak key derivation algorithm and iteration count
        var key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(settings.Value.SsnPassphrase),
            bytes.AsSpan(0, SaltSize),
            Iterations,
            HashAlgorithmName.SHA1,
            KeySize);
#pragma warning restore CA5379, CA5387, S4790, S5344

        using var aes = Aes.Create();
        aes.Key = key;

#pragma warning disable CA5401, S5542 // CBC without authentication is the legacy format
        var plaintext = aes.DecryptCbc(bytes.AsSpan(SaltSize + IvSize), bytes.AsSpan(SaltSize, IvSize), PaddingMode.PKCS7);
#pragma warning restore CA5401, S5542

        return Encoding.UTF8.GetString(plaintext);
    }
}
