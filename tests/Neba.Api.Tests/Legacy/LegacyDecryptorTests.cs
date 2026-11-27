using System.Security.Cryptography;

using Microsoft.Extensions.Options;

using Neba.Api.Legacy;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy;

[UnitTest]
[Component("Legacy")]
public sealed class LegacyDecryptorTests
{
    // Made up. The real passphrase never goes in the repo.
    private const string FakePassphrase = "fake-legacy-passphrase-for-tests";

    private static LegacyDecryptor CreateSut(string passphrase = FakePassphrase)
        => new(Options.Create(new LegacySettings { SsnPassphrase = passphrase }));

    [Fact(DisplayName = "Empty input should give empty output")]
    public void Decrypt_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.Decrypt(string.Empty);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "A value too short to hold a salt, IV and ciphertext should throw")]
    public void Decrypt_ShouldThrow_WhenValueIsTooShort()
    {
        // Arrange
        var sut = CreateSut();
        var value = Convert.ToBase64String(new byte[32]);

        // Act
        var act = () => sut.Decrypt(value);

        // Assert
        act.ShouldThrow<CryptographicException>();
    }

    [Fact(DisplayName = "A value that is not Base64 should throw")]
    public void Decrypt_ShouldThrow_WhenValueIsNotBase64()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Decrypt("900-12-3456!");

        // Assert
        act.ShouldThrow<FormatException>();
    }
}
