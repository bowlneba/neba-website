using Neba.TestFactory.Attributes;
using Neba.Website.Server.Maps;

namespace Neba.Website.Tests.Maps;

[UnitTest]
[Component("Website.Maps.MapBounds")]
public sealed class MapBoundsTests
{
    [Fact(DisplayName = "Contains returns true when coordinate is inside bounds")]
    public void Contains_ShouldReturnTrue_WhenCoordinateIsInsideBounds()
    {
        var bounds = new MapBounds(North: 43.0, South: 42.0, East: -70.0, West: -72.0);

        bounds.Contains(42.5, -71.0).ShouldBeTrue();
    }

    [Fact(DisplayName = "Contains returns false when latitude is north of bounds")]
    public void Contains_ShouldReturnFalse_WhenLatitudeIsNorthOfBounds()
    {
        var bounds = new MapBounds(North: 43.0, South: 42.0, East: -70.0, West: -72.0);

        bounds.Contains(43.5, -71.0).ShouldBeFalse();
    }

    [Fact(DisplayName = "Contains returns false when latitude is south of bounds")]
    public void Contains_ShouldReturnFalse_WhenLatitudeIsSouthOfBounds()
    {
        var bounds = new MapBounds(North: 43.0, South: 42.0, East: -70.0, West: -72.0);

        bounds.Contains(41.5, -71.0).ShouldBeFalse();
    }

    [Fact(DisplayName = "Contains returns false when longitude is east of bounds")]
    public void Contains_ShouldReturnFalse_WhenLongitudeIsEastOfBounds()
    {
        var bounds = new MapBounds(North: 43.0, South: 42.0, East: -70.0, West: -72.0);

        bounds.Contains(42.5, -69.0).ShouldBeFalse();
    }

    [Fact(DisplayName = "Contains returns false when longitude is west of bounds")]
    public void Contains_ShouldReturnFalse_WhenLongitudeIsWestOfBounds()
    {
        var bounds = new MapBounds(North: 43.0, South: 42.0, East: -70.0, West: -72.0);

        bounds.Contains(42.5, -73.0).ShouldBeFalse();
    }

    [Fact(DisplayName = "Contains returns true when coordinate is exactly on a boundary")]
    public void Contains_ShouldReturnTrue_WhenCoordinateIsOnBoundary()
    {
        var bounds = new MapBounds(North: 43.0, South: 42.0, East: -70.0, West: -72.0);

        bounds.Contains(43.0, -72.0).ShouldBeTrue();
    }
}