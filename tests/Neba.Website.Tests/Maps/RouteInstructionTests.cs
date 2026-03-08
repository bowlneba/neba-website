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
        var instruction = new RouteInstruction { DistanceMeters = 50 }; // ~164 ft

        instruction.FormattedDistance.ShouldEndWith(" ft");
    }

    [Fact(DisplayName = "FormattedDistance returns miles when distance is 0.1 miles or more")]
    public void FormattedDistance_ShouldReturnMiles_WhenDistanceIsAtLeast0Point1Miles()
    {
        var instruction = new RouteInstruction { DistanceMeters = 1000 }; // ~0.6 mi

        instruction.FormattedDistance.ShouldEndWith(" mi");
    }

    [Fact(DisplayName = "FormattedDistance formats feet with no decimal places")]
    public void FormattedDistance_ShouldFormatFeet_WithNoDecimalPlaces()
    {
        var instruction = new RouteInstruction { DistanceMeters = 50 };
        var result = instruction.FormattedDistance;

        result.ShouldEndWith(" ft");
        result.ShouldNotContain(".");
    }

    [Fact(DisplayName = "FormattedDistance formats miles with one decimal place")]
    public void FormattedDistance_ShouldFormatMiles_WithOneDecimalPlace()
    {
        var instruction = new RouteInstruction { DistanceMeters = 1609.34 }; // exactly 1 mile

        instruction.FormattedDistance.ShouldBe("1.0 mi");
    }
}