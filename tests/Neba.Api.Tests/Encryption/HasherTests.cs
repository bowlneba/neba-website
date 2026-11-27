using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

[UnitTest]
[Component("Encryption")]
public sealed class HasherTests
{
    private static readonly byte[] HashKey = Encoding.UTF8.GetBytes("test-hash-key-of-32-bytes-......");

    // Fake SSNs start with 9, which is never issued as one.
    private const string FakeSsn = "900123456";

    [Fact(DisplayName = "The same input and key should give the same HMAC-SHA256 hash")]
    public void Hash_ShouldReturnSameHash_WhenInputAndKeyMatch()
    {
        // Arrange
        var expected = Convert.ToBase64String(HMACSHA256.HashData(HashKey, Encoding.UTF8.GetBytes(FakeSsn)));

        // Act
        var first = new Hasher<BowlerPii>(HashKey).Hash(FakeSsn);
        var second = new Hasher<BowlerPii>(HashKey).Hash(FakeSsn);

        // Assert
        first.ShouldBe(second);
        first.Value.ShouldBe(expected);
    }

    [Fact(DisplayName = "A different key should give a different hash")]
    public void Hash_ShouldReturnDifferentHash_WhenKeyDiffers()
    {
        // Arrange
        var otherKey = Encoding.UTF8.GetBytes("another-hash-key-of-32-bytes-...");

        // Act
        var first = new Hasher<BowlerPii>(HashKey).Hash(FakeSsn);
        var second = new Hasher<BowlerPii>(otherKey).Hash(FakeSsn);

        // Assert
        first.Value.ShouldNotBe(second.Value);
    }

    [Fact(DisplayName = "Serializing a hash should throw, alone or inside another object")]
    public void Serialize_ShouldThrow_ForAHash()
    {
        // Arrange
        var hash = new Hasher<BowlerPii>(HashKey).Hash(FakeSsn);

        // Act
        var alone = () => JsonSerializer.Serialize(hash);
        var nested = () => JsonSerializer.Serialize(new { Hash = hash });
        var read = () => JsonSerializer.Deserialize<Hashed<BowlerPii>>("\"abc\"");

        // Assert
        alone.ShouldThrow<NotSupportedException>();
        nested.ShouldThrow<NotSupportedException>();
        read.ShouldThrow<NotSupportedException>();
    }

    [Fact(DisplayName = "ToString should not show the hash")]
    public void ToString_ShouldBeRedacted()
    {
        // Arrange
        var hash = new Hasher<BowlerPii>(HashKey).Hash(FakeSsn);

        // Act
        var text = $"{hash}";

        // Assert
        text.ShouldBe("[REDACTED]");
    }

    [Fact(DisplayName = "A hash should have no public properties for serializers or loggers to read")]
    public void Hashed_ShouldHaveNoPublicProperties()
    {
        // Arrange
        var type = typeof(Hashed<BowlerPii>);

        // Act
        var properties = type.GetProperties();

        // Assert
        properties.ShouldBeEmpty();
    }
}