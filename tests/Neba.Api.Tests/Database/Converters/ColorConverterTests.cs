using System.Drawing;

using Neba.Api.Database.Converters;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Converters;

[UnitTest]
[Component("Database.Converters")]
public sealed class ColorConverterTests
{
    [Fact(DisplayName = "Should convert color to ARGB int value")]
    public void ConvertToProvider_ShouldReturnArgbInt_WhenColorIsProvided()
    {
        // Arrange
        var color = Color.FromArgb(255, 100, 150, 200);
        var converter = new Api.Database.Converters.ColorConverter();

        // Act
        var result = (int)converter.ConvertToProvider(color)!;

        // Assert
        result.ShouldBe(color.ToArgb());
    }

    [Fact(DisplayName = "Should convert fully transparent color to zero")]
    public void ConvertToProvider_ShouldReturnZero_WhenColorIsFullyTransparent()
    {
        // Arrange
        var color = Color.FromArgb(0, 0, 0, 0);
        var converter = new Api.Database.Converters.ColorConverter();

        // Act
        var result = (int)converter.ConvertToProvider(color)!;

        // Assert
        result.ShouldBe(0);
    }

    [Fact(DisplayName = "Should convert ARGB int to color with matching ARGB components")]
    public void ConvertFromProvider_ShouldReturnColorWithMatchingArgb_WhenArgbValueIsProvided()
    {
        // Arrange
        var original = Color.FromArgb(200, 75, 125, 175);
        var argbValue = original.ToArgb();
        var converter = new Api.Database.Converters.ColorConverter();

        // Act
        var result = (Color)converter.ConvertFromProvider(argbValue)!;

        // Assert
        result.ToArgb().ShouldBe(argbValue);
    }

    [Fact(DisplayName = "Should preserve ARGB components when converting to and from provider")]
    public void RoundTrip_ShouldPreserveArgbComponents_WhenConvertingToAndFromProvider()
    {
        // Arrange
        var original = Color.FromArgb(128, 50, 100, 200);
        var converter = new Api.Database.Converters.ColorConverter();

        // Act
        var providerValue = (int)converter.ConvertToProvider(original)!;
        var result = (Color)converter.ConvertFromProvider(providerValue)!;

        // Assert
        result.A.ShouldBe(original.A);
        result.R.ShouldBe(original.R);
        result.G.ShouldBe(original.G);
        result.B.ShouldBe(original.B);
    }
}