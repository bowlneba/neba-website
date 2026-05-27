using Neba.TestFactory.Attributes;
using Neba.Website.Server.Contact;

namespace Neba.Website.Tests.Contact;

[UnitTest]
[Component("Website.Contact.PostalCodeFormatter")]
public sealed class PostalCodeFormatterTests
{
    [Theory(DisplayName = "Formats postal code correctly")]
    [InlineData("123456789", "12345-6789")]
    [InlineData("900010000", "90001-0000")]
    [InlineData("A1B2C3", "A1B 2C3")]
    [InlineData("K1A0A9", "K1A 0A9")]
    public void FormatForDisplay_ShouldFormat_WhenPostalCodeNeedsFormatting(string postalCode, string expected)
    {
        // Act
        var result = PostalCodeFormatter.FormatForDisplay(postalCode);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory(DisplayName = "Returns postal code as-is when already formatted or unrecognized")]
    [InlineData("12345")]
    [InlineData("12345-6789")]
    [InlineData("K1A 0A9")]
    [InlineData("123456")]
    [InlineData("A1B2C3D")]
    public void FormatForDisplay_ShouldReturnAsIs_WhenPostalCodeNeedsNoFormatting(string postalCode)
    {
        // Act
        var result = PostalCodeFormatter.FormatForDisplay(postalCode);

        // Assert
        result.ShouldBe(postalCode);
    }

#nullable disable
    [Theory(DisplayName = "Throws ArgumentException when postal code is null or whitespace")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FormatForDisplay_ShouldThrow_WhenPostalCodeIsNullOrWhiteSpace(string postalCode)
    {
        // Act
        var act = () => PostalCodeFormatter.FormatForDisplay(postalCode);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }
#nullable enable
}