using Neba.Api.Database.Converters;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Converters;

[UnitTest]
[Component("Database.Converters")]
public sealed class UlidConverterTests
{
    [Fact(DisplayName = "Should convert Ulid to its string representation")]
    public void ConvertToProvider_ShouldReturnUlidString_WhenValidUlid()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var converter = new UlidConverter();

        // Act
        var result = converter.ConvertToProvider(ulid);

        // Assert
        result.ShouldBe(ulid.ToString());
    }

    [Fact(DisplayName = "Should convert ULID string to Ulid")]
    public void ConvertFromProvider_ShouldReturnUlid_WhenValidUlidString()
    {
        // Arrange
        var ulid = Ulid.NewUlid();
        var converter = new UlidConverter();

        // Act
        var result = converter.ConvertFromProvider(ulid.ToString());

        // Assert
        result.ShouldBeOfType<Ulid>();
        ((Ulid)result!).ShouldBe(ulid);
    }

    [Fact(DisplayName = "Should preserve value when converting to and from string")]
    public void RoundTrip_ShouldPreserveValue_WhenConvertingToAndFromString()
    {
        // Arrange
        var original = Ulid.NewUlid();
        var converter = new UlidConverter();

        // Act
        var asString = (string)converter.ConvertToProvider(original)!;
        var result = (Ulid)converter.ConvertFromProvider(asString)!;

        // Assert
        result.ShouldBe(original);
    }

    [Fact(DisplayName = "Should convert empty Ulid to empty ULID string")]
    public void ConvertToProvider_ShouldReturnEmptyUlidString_WhenEmptyUlid()
    {
        // Arrange
        var converter = new UlidConverter();

        // Act
        var result = converter.ConvertToProvider(Ulid.Empty);

        // Assert
        result.ShouldBe(Ulid.Empty.ToString());
    }

    [Fact(DisplayName = "Converted string should always be 26 characters")]
    public void ConvertToProvider_ShouldReturn26CharString_WhenAnyUlid()
    {
        // Arrange
        var converter = new UlidConverter();

        // Act
        var result = (string)converter.ConvertToProvider(Ulid.NewUlid())!;

        // Assert
        result.Length.ShouldBe(26);
    }
}
