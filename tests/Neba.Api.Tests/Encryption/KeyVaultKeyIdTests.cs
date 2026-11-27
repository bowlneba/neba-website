using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

[UnitTest]
[Component("Encryption")]
public sealed class KeyVaultKeyIdTests
{
    private const string Version = "0123456789abcdef0123456789abcdef";

    [Fact(DisplayName = "A versioned key URI should split into vault, name and version")]
    public void TryParse_ShouldReturnParts_WhenUriHasAVersion()
    {
        // Arrange
        const string uri = $"https://vault.vault.azure.net/keys/bowlerpii-key/{Version}";

        // Act
        var parsed = KeyVaultKeyId.TryParse(uri, out var keyId);

        // Assert
        parsed.ShouldBeTrue();
        keyId!.VaultUri.ShouldBe(new Uri("https://vault.vault.azure.net/"));
        keyId.KeyName.ShouldBe("bowlerpii-key");
        keyId.Version.ShouldBe(Version);
        keyId.ForVersion(Version).ShouldBe(new Uri(uri));
    }

    [Theory(DisplayName = "A URI without a usable version should be rejected")]
    [InlineData(null, TestDisplayName = "Null")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("not a uri", TestDisplayName = "Not a URI")]
    [InlineData("http://vault.vault.azure.net/keys/k/0123456789abcdef0123456789abcdef", TestDisplayName = "Not https")]
    [InlineData("https://vault.vault.azure.net/keys/k", TestDisplayName = "No version")]
    [InlineData("https://vault.vault.azure.net/secrets/k/0123456789abcdef0123456789abcdef", TestDisplayName = "Not a key")]
    [InlineData("https://vault.vault.azure.net/keys/k/short", TestDisplayName = "Short version")]
    public void TryParse_ShouldReturnFalse_WhenUriIsNotAVersionedKey(string? value)
    {
        // Arrange

        // Act
        var parsed = KeyVaultKeyId.TryParse(value, out var keyId);

        // Assert
        parsed.ShouldBeFalse();
        keyId.ShouldBeNull();
    }

    [Theory(DisplayName = "Only 32 hex characters should count as a version")]
    [InlineData("0123456789ABCDEF0123456789abcdef", true, TestDisplayName = "Mixed case hex")]
    [InlineData("local", false, TestDisplayName = "Local")]
    [InlineData("../../secrets/x", false, TestDisplayName = "Path traversal")]
    [InlineData("0123456789abcdef0123456789abcdef\n", false, TestDisplayName = "Trailing newline")]
    [InlineData("0123456789abcdef0123456789abcdeg", false, TestDisplayName = "Not hex")]
    public void IsValidVersion_ShouldAcceptOnlyKeyVaultVersions(string version, bool expected)
    {
        // Arrange

        // Act
        var result = KeyVaultKeyId.IsValidVersion(version);

        // Assert
        result.ShouldBe(expected);
    }
}