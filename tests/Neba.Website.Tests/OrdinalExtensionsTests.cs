using System.Globalization;

using Neba.TestFactory.Attributes;
using Neba.Website.Server;

namespace Neba.Website.Tests;

[UnitTest]
[Component("Website.DisplayCulture")]
public sealed class OrdinalExtensionsTests
{
    [Theory(DisplayName = "ToOrdinal should format the number as an English ordinal")]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(22, "22nd")]
    [InlineData(101, "101st")]
    public void ToOrdinal_ShouldReturnEnglishOrdinal(int number, string expected)
    {
        // Arrange & Act
        var result = number.ToOrdinal();

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "ToOrdinal should use English regardless of the current culture")]
    public void ToOrdinal_ShouldUseEnglish_WhenCurrentCultureIsNotEnglish()
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

        try
        {
            // Act
            var result = 1.ToOrdinal();

            // Assert
            result.ShouldBe("1st");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }
}
