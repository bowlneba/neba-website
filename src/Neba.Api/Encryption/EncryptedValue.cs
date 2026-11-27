namespace Neba.Api.Encryption;

/// <summary>
/// A sensitive value as an entity stores it: the ciphertext, and the keyed hash that finds duplicates.
/// The two are always set together. Holds no plaintext.
/// </summary>
internal sealed record EncryptedValue<TKey> where TKey : IEncryptionKey
{
    public EncryptedValue(Encrypted<TKey> encrypted, Hashed<TKey> hash)
    {
        ArgumentNullException.ThrowIfNull(encrypted);
        ArgumentNullException.ThrowIfNull(hash);

        Value = encrypted.Value;
        Hash = hash.Value;
    }

    // For EF Core.
    private EncryptedValue()
    {
    }

    // Internal, not public, so serializers and logging that read public properties see neither.
    // Strings, not Encrypted/Hashed, so the mapping needs no EF value converter.
    internal string Value { get; private init; } = string.Empty;

    internal string Hash { get; private init; } = string.Empty;

    /// <summary>
    /// The ciphertext, typed for <see cref="IEncryptor{TKey}.DecryptAsync"/>.
    /// </summary>
    public Encrypted<TKey> ToEncrypted() => new(Value);

    public override string ToString() => "[REDACTED]";
}