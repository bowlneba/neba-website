namespace Neba.Api.Encryption;

/// <summary>
/// Settings for one key, from <c>Encryption:Keys:{Name}</c>.
/// </summary>
// A class, not a record: a record would print the secrets from ToString().
internal sealed class EncryptionKeySettings
{
    /// <summary>Versioned Key Vault key URI. Required outside Development.</summary>
    public string KeyVaultKeyUri { get; init; } = string.Empty;

    /// <summary>Base64 PKCS#8 RSA private key. Development only.</summary>
    public string LocalPrivateKey { get; init; } = string.Empty;

    /// <summary>Base64 HMAC key of at least 32 bytes.</summary>
    public string HashKey { get; init; } = string.Empty;
}