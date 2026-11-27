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

    // Made by the Software's Encrypt code in LINQPad 5 on .NET Framework 4.8.9363.0, with the fake
    // passphrase above and made-up SSNs. Do not replace them with output from newer code: that
    // would only test the decryptor against itself.
    [Theory(DisplayName = "A sample made by the old code on .NET Framework should decrypt")]
    [InlineData("2Rm1YVVEhWqEp24DJPOrjxYo0ic5GuWmtn4VWRLwClRLSwTstCRifzT2qXnsfxIh", "900123456", TestDisplayName = "Nine digits")]
    [InlineData("D2TMFxn4jVylfBAG40STSYQMjKYc3frJx+0My3nxn5kvBet367H3ZSxFsDrC5KmQ", "900-12-3456", TestDisplayName = "Dashes")]
    [InlineData("qkHKD2Z88hY1kF8woMT+J/YtgLAnHrsZqUT0bxG87/FjYEpEmfELCTJuX+jGQFVa", "987654321", TestDisplayName = "Other digits")]
    [InlineData("XWOVIkH2Ruh4pCv+cwKH4KeDN/Sv2Aqh5krVA34mzFIDuoY86k9t3rGOUE8z7IGa", "900 12 3456", TestDisplayName = "Spaces")]
    [InlineData("9vaBaNm9D1GShGzEw+B7EunA/cCEyIInv2bR+3kf4zq7h4Zt33PKrpglitQasGCJ0w6VNiEo/IIjIyhv6ExQqo8cZjp5GqJm8zFCbvfQ4nE=", "made-up value longer than one AES block", TestDisplayName = "Longer than one block")]
    public void Decrypt_ShouldReturnPlaintext_WhenSampleCameFromTheOldCode(string cipherText, string expected)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.Decrypt(cipherText);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "A wrong passphrase should not give the plaintext")]
    public void Decrypt_ShouldNotReturnPlaintext_WhenPassphraseIsWrong()
    {
        // Arrange
        var sut = CreateSut("not-the-passphrase");
        var result = string.Empty;

        // Act: usually bad padding throws; by chance it can give garbage instead.
        var exception = Record.Exception(
            () => result = sut.Decrypt("2Rm1YVVEhWqEp24DJPOrjxYo0ic5GuWmtn4VWRLwClRLSwTstCRifzT2qXnsfxIh"));

        // Assert
        if (exception is not null)
        {
            exception.ShouldBeOfType<CryptographicException>();
        }

        result.ShouldNotBe("900123456");
    }

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
