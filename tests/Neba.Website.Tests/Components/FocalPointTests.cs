using System.Globalization;


using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.FocalPoint")]
public sealed class FocalPointTests
{
    [Fact(DisplayName = "Default should be the center of the photo with no zoom")]
    public void Default_ShouldBeCenteredWithNoZoom()
    {
        // Arrange
        // Act
        var point = FocalPoint.Default;

        // Assert
        point.ShouldBe(new FocalPoint(50, 50, 100));
    }

    [Theory(DisplayName = "ToImageStyle should position and scale the photo about the focal point")]
    [InlineData(50, 50, 100, "object-position:50% 50%;transform-origin:50% 50%;transform:scale(1)")]
    [InlineData(0, 100, 300, "object-position:0% 100%;transform-origin:0% 100%;transform:scale(3)")]
    [InlineData(31, 36, 175, "object-position:31% 36%;transform-origin:31% 36%;transform:scale(1.75)")]
    public void ToImageStyle_ShouldPositionAndScale(int x, int y, int zoom, string expected)
    {
        // Arrange
        var point = new FocalPoint(x, y, zoom);

        // Act
        var style = point.ToImageStyle();

        // Assert
        style.ShouldBe(expected);
    }

    [Fact(DisplayName = "ToImageStyle should use a period as the decimal separator in any culture")]
    public void ToImageStyle_ShouldUseInvariantCulture()
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            // Act
            var style = new FocalPoint(50, 50, 150).ToImageStyle();

            // Assert
            style.ShouldEndWith("scale(1.5)");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
