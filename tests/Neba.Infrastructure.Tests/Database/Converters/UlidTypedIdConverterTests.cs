using Neba.Infrastructure.Database.Converters;
using Neba.TestFactory.Attributes;

using StronglyTypedIds;

namespace Neba.Infrastructure.Tests.Database.Converters;

[UnitTest]
[Component("Database.Converters")]
public sealed partial class UlidTypedIdConverterTests
{
    [StronglyTypedId("ulid-full")]
    private readonly partial struct TestId;

    private readonly struct NoStringCtorId(int value)
    {
        public int Value { get; } = value;
    }

    [Fact(DisplayName = "Should convert typed ID to ULID string")]
    public void ConvertToProvider_ShouldReturnUlidString_WhenValidId()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var id = new TestId(ulid.ToString());
        var converter = new UlidTypedIdConverter<TestId>();

        // Act
        var result = converter.ConvertToProvider(id);

        // Assert
        result.ShouldBe(ulid.ToString());
    }

    [Fact(DisplayName = "Should convert ULID string to typed ID")]
    public void ConvertFromProvider_ShouldReturnTypedId_WhenValidUlidString()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var converter = new UlidTypedIdConverter<TestId>();

        // Act
        var result = converter.ConvertFromProvider(ulid.ToString());

        // Assert
        result.ShouldBeOfType<TestId>();
        ((TestId)result!).Value.ShouldBe(ulid);
    }

    [Fact(DisplayName = "Should preserve value when converting to and from string")]
    public void RoundTrip_ShouldPreserveValue_WhenConvertingToAndFromString()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var original = new TestId(ulid.ToString());
        var converter = new UlidTypedIdConverter<TestId>();

        // Act
        var asString = (string)converter.ConvertToProvider(original)!;
        var result = (TestId)converter.ConvertFromProvider(asString)!;

        // Assert
        result.ShouldBe(original);
    }

    [Fact(DisplayName = "Should convert empty typed ID to empty ULID string")]
    public void ConvertToProvider_ShouldReturnEmptyUlidString_WhenEmptyId()
    {
        // Arrange
        var converter = new UlidTypedIdConverter<TestId>();

        // Act
        var result = converter.ConvertToProvider(TestId.Empty);

        // Assert
        result.ShouldBe(Ulid.Empty.ToString());
    }

    [Fact(DisplayName = "Should throw InvalidOperationException when type has no string constructor")]
    public void Constructor_ShouldThrow_WhenTypeHasNoStringConstructor()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => new UlidTypedIdConverter<NoStringCtorId>());
    }
}
