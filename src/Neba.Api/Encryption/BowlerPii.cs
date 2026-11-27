namespace Neba.Api.Encryption;

/// <summary>
/// The key for bowler personal data, such as SSNs.
/// </summary>
internal sealed class BowlerPii : IEncryptionKey
{
    public static string Name => "BowlerPii";
}