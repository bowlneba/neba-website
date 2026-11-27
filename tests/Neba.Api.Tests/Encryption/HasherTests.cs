using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Bogus;
using Bogus.Extensions.UnitedStates;

using Neba.Api.Encryption;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Encryption;

[UnitTest]
[Component("Encryption")]
public sealed class HasherTests
{
    private static readonly byte[] HashKey = Encoding.UTF8.GetBytes("test-hash-key-of-32-bytes-......");

    private static string FakeSsn(int seed)
        => new Faker { Random = new Randomizer(seed) }.Person.Ssn().Replace("-", string.Empty, StringComparison.Ordinal);

    [Fact(DisplayName = "The same input and key should give the same HMAC-SHA256 hash")]
    public void Hash_ShouldReturnSameHash_WhenInputAndKeyMatch()
    {
        // Arrange
        var ssn = FakeSsn(8101);
        var expected = Convert.ToBase64String(HMACSHA256.HashData(HashKey, Encoding.UTF8.GetBytes(ssn)));

        // Act
        var first = new Hasher<BowlerPii>(HashKey).Hash(ssn);
        var second = new Hasher<BowlerPii>(HashKey).Hash(ssn);

        // Assert
        first.ShouldBe(second);
        first.Value.ShouldBe(expected);
    }

    [Fact(DisplayName = "A different key should give a different hash")]
    public void Hash_ShouldReturnDifferentHash_WhenKeyDiffers()
    {
        // Arrange
        var ssn = FakeSsn(8102);
        var otherKey = Encoding.UTF8.GetBytes("another-hash-key-of-32-bytes-...");

        // Act
        var first = new Hasher<BowlerPii>(HashKey).Hash(ssn);
        var second = new Hasher<BowlerPii>(otherKey).Hash(ssn);

        // Assert
        first.Value.ShouldNotBe(second.Value);
    }

    [Fact(DisplayName = "Serializing a hash should throw, alone or inside another object")]
    public void Serialize_ShouldThrow_ForAHash()
    {
        // Arrange
        var hash = new Hasher<BowlerPii>(HashKey).Hash(FakeSsn(8103));

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
        var hash = new Hasher<BowlerPii>(HashKey).Hash(FakeSsn(8104));

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
