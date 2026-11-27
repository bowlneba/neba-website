using Neba.Api.Encryption;

namespace Neba.Api.Tests.Encryption;

/// <summary>A second key marker. Production has no second key.</summary>
internal sealed class OtherPii : IEncryptionKey
{
    public static string Name => "OtherPii";
}