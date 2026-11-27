namespace Neba.Api.Encryption;

/// <summary>
/// Hashes values with the secret hash key of <typeparamref name="TKey"/>.
/// </summary>
internal interface IHasher<TKey> where TKey : IEncryptionKey
{
    Hashed<TKey> Hash(string value);
}