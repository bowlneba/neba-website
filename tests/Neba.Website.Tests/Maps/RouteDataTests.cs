using Neba.TestFactory.Attributes;
using Neba.Website.Server.Maps;

using RouteData = Neba.Website.Server.Maps.RouteData;

namespace Neba.Website.Tests.Maps;

[UnitTest]
[Component("Website.Maps.RouteData")]
public sealed class RouteDataTests
{
    [Fact(DisplayName = "FormattedDistance returns miles with one decimal place")]
    public void FormattedDistance_ShouldReturnMilesWithOneDecimal()
    {
        var route = new RouteData { DistanceMeters = 16093.4 }; // ~10.0 mi

        route.FormattedDistance.ShouldBe("10.0 mi");
    }

    [Fact(DisplayName = "FormattedTravelTime returns minutes when travel time is under 60 minutes")]
    public void FormattedTravelTime_ShouldReturnMinutes_WhenUnder60Minutes()
    {
        var route = new RouteData { TravelTimeSeconds = 1200 }; // 20 min

        route.FormattedTravelTime.ShouldBe("20 min");
    }

    [Fact(DisplayName = "FormattedTravelTime returns zero minutes when travel time is zero")]
    public void FormattedTravelTime_ShouldReturn0Min_WhenTravelTimeIsZero()
    {
        var route = new RouteData { TravelTimeSeconds = 0 };

        route.FormattedTravelTime.ShouldBe("0 min");
    }

    [Fact(DisplayName = "FormattedTravelTime returns hours and minutes when travel time is 60 minutes or more")]
    public void FormattedTravelTime_ShouldReturnHoursAndMinutes_WhenAtLeast60Minutes()
    {
        var route = new RouteData { TravelTimeSeconds = 5400 }; // 1 hr 30 min

        route.FormattedTravelTime.ShouldBe("1 hr 30 min");
    }

    [Fact(DisplayName = "FormattedTravelTime returns hours with zero remaining minutes when exactly on the hour")]
    public void FormattedTravelTime_ShouldReturnHoursAndZeroMinutes_WhenExactlyOnHour()
    {
        var route = new RouteData { TravelTimeSeconds = 3600 }; // 1 hr 0 min

        route.FormattedTravelTime.ShouldBe("1 hr 0 min");
    }

    [Fact(DisplayName = "FormattedTravelTime handles multiple hours correctly")]
    public void FormattedTravelTime_ShouldHandleMultipleHours()
    {
        var route = new RouteData { TravelTimeSeconds = 9000 }; // 2 hr 30 min

        route.FormattedTravelTime.ShouldBe("2 hr 30 min");
    }
}