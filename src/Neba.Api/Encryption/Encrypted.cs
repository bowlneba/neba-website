namespace Neba.Api.Encryption;

/// <summary>
/// A value encrypted for storage with <typeparamref name="TKey"/>.
/// </summary>
// TKey is unused in the body on purpose: it stops a value from going to another key's encryptor.
#pragma warning disable S2326 // Unused type parameters should be removed
internal sealed record Encrypted<TKey>(string Value) where TKey : IEncryptionKey;
#pragma warning restore S2326
