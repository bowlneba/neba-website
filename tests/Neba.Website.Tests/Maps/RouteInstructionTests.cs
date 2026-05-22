using Neba.TestFactory.Attributes;
using Neba.Website.Server.Maps;

namespace Neba.Website.Tests.Maps;

[UnitTest]
[Component("Website.Maps.RouteInstruction")]
public sealed class RouteInstructionTests
{
    [Fact(DisplayName = "FormattedDistance returns feet when distance is under 0.1 miles")]
    public void FormattedDistance_ShouldReturnFeet_WhenDistanceIsUnder0Point1Miles()
    {
        // Arrange
        var instruction = new RouteInstruction { DistanceMeters = 50 }; // ~164 ft

        // Assert
        instruction.FormattedDistance.ShouldEndWith(" ft");
    }

    [Fact(DisplayName = "FormattedDistance returns miles when distance is 0.1 miles or more")]
    public void FormattedDistance_ShouldReturnMiles_WhenDistanceIsAtLeast0Point1Miles()
    {
        // Arrange
        var instruction = new RouteInstruction { DistanceMeters = 1000 }; // ~0.6 mi

        // Assert
        instruction.FormattedDistance.ShouldEndWith(" mi");
    }

    [Fact(DisplayName = "FormattedDistance formats feet with no decimal places")]
    public void FormattedDistance_ShouldFormatFeet_WithNoDecimalPlaces()
    {
        // Arrange
        var instruction = new RouteInstruction { DistanceMeters = 50 };

        // Act
        var result = instruction.FormattedDistance;

        // Assert
        result.ShouldEndWith(" ft");
        result.ShouldNotContain(".");
    }

    [Fact(DisplayName = "FormattedDistance formats miles with one decimal place")]
    public void FormattedDistance_ShouldFormatMiles_WithOneDecimalPlace()
    {
        // Arrange
        var instruction = new RouteInstruction { DistanceMeters = 1609.34 }; // exactly 1 mile

        // Assert
        instruction.FormattedDistance.ShouldBe("1.0 mi");
    }

    [Fact(DisplayName = "FormattedDistance returns miles when distance is exactly 0.1 miles")]
    public void FormattedDistance_ShouldReturnMiles_WhenDistanceIsExactly0Point1Miles()
    {
        // 0.1 miles = 160.9344 meters; boundary test: >= 0.1 returns miles, < 0.1 returns feet
        // Arrange
        var instruction = new RouteInstruction { DistanceMeters = 160.9344 };

        // Assert
        instruction.FormattedDistance.ShouldBe("0.1 mi");
    }
}