using System.Text.Json;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

[UnitTest]
[Component("Encryption")]
public sealed class EncryptedValueTests
{
    private static EncryptedValue<BowlerPii> Create()
        => new(new Encrypted<BowlerPii>("cipher-text"), new Hashed<BowlerPii>("hash-text"));

    [Fact(DisplayName = "It should hold the ciphertext and hash it was given")]
    public void Constructor_ShouldHoldBothParts()
    {
        // Arrange

        // Act
        var result = Create();

        // Assert
        result.Value.ShouldBe("cipher-text");
        result.Hash.ShouldBe("hash-text");
        result.ToEncrypted().Value.ShouldBe("cipher-text");
    }

    [Fact(DisplayName = "Neither ToString nor JSON should show the ciphertext or hash")]
    public void ToStringAndSerialize_ShouldShowNothing()
    {
        // Arrange
        var value = Create();

        // Act
        var text = $"{value}";
        var json = JsonSerializer.Serialize(value);

        // Assert
        text.ShouldBe("[REDACTED]");
        json.ShouldBe("{}");
    }

#nullable disable
    [Fact(DisplayName = "A missing ciphertext should be rejected")]
    public void Constructor_ShouldThrow_WhenEncryptedIsNull()
    {
        // Arrange
        var hash = new Hashed<BowlerPii>("hash-text");

        // Act
        var act = () => new EncryptedValue<BowlerPii>(null, hash);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("encrypted");
    }

    [Fact(DisplayName = "A missing hash should be rejected")]
    public void Constructor_ShouldThrow_WhenHashIsNull()
    {
        // Arrange
        var encrypted = new Encrypted<BowlerPii>("cipher-text");

        // Act
        var act = () => new EncryptedValue<BowlerPii>(encrypted, null);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("hash");
    }
#nullable enable
}