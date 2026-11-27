namespace Neba.Api.Encryption;

/// <summary>
/// Marks an encryption key. One key covers one data boundary.
/// </summary>
internal interface IEncryptionKey
{
    /// <summary>
    /// The key name, used in configuration and bound into every encrypted value.
    /// </summary>
    static abstract string Name { get; }
}
