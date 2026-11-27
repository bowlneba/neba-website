using System.Security.Cryptography;
using System.Text;

namespace Neba.Api.Encryption;

/// <summary>
/// HMAC-SHA256. No per-row salt, or duplicates would not match; the secret key protects the hash.
/// Changing the key means decrypting every value and hashing it again.
/// </summary>
internal sealed class Hasher<TKey>(byte[] hashKey) : IHasher<TKey> where TKey : IEncryptionKey
{
    public Hashed<TKey> Hash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Hashed<TKey>(Convert.ToBase64String(HMACSHA256.HashData(hashKey, Encoding.UTF8.GetBytes(value))));
    }
}
