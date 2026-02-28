using Neba.Domain.Geography;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Geography;

[UnitTest]
[Component("Geography")]
public sealed class CoordinatesTests
{
    #region Create - valid

    [Fact(DisplayName = "Create returns Coordinates with correct latitude and longitude when values are valid")]
    public void Create_ShouldReturnCoordinates_WhenValuesAreValid()
    {
        var result = Coordinates.Create(41.7508, -72.6850);

        result.IsError.ShouldBeFalse();
        result.Value.Latitude.ShouldBe(41.7508);
        result.Value.Longitude.ShouldBe(-72.6850);
    }

    [Fact(DisplayName = "Create succeeds at latitude lower boundary of -90")]
    public void Create_ShouldSucceed_WhenLatitudeIsNegative90()
    {
        var result = Coordinates.Create(-90, 0);

        result.IsError.ShouldBeFalse();
        result.Value.Latitude.ShouldBe(-90);
    }

    [Fact(DisplayName = "Create succeeds at latitude upper boundary of 90")]
    public void Create_ShouldSucceed_WhenLatitudeIs90()
    {
        var result = Coordinates.Create(90, 0);

        result.IsError.ShouldBeFalse();
        result.Value.Latitude.ShouldBe(90);
    }

    [Fact(DisplayName = "Create succeeds at longitude lower boundary of -180")]
    public void Create_ShouldSucceed_WhenLongitudeIsNegative180()
    {
        var result = Coordinates.Create(0, -180);

        result.IsError.ShouldBeFalse();
        result.Value.Longitude.ShouldBe(-180);
    }

    [Fact(DisplayName = "Create succeeds at longitude upper boundary of 180")]
    public void Create_ShouldSucceed_WhenLongitudeIs180()
    {
        var result = Coordinates.Create(0, 180);

        result.IsError.ShouldBeFalse();
        result.Value.Longitude.ShouldBe(180);
    }

    [Fact(DisplayName = "Create succeeds at origin (0, 0)")]
    public void Create_ShouldSucceed_WhenLatitudeAndLongitudeAreZero()
    {
        var result = Coordinates.Create(0, 0);

        result.IsError.ShouldBeFalse();
    }

    #endregion

    #region Create - invalid latitude

    [Fact(DisplayName = "Create returns InvalidLatitude error when latitude is below -90")]
    public void Create_ShouldReturnInvalidLatitudeError_WhenLatitudeIsBelowNegative90()
    {
        var result = Coordinates.Create(-90.001, 0);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Coordinates.InvalidLatitude");
    }

    [Fact(DisplayName = "Create returns InvalidLatitude error when latitude is above 90")]
    public void Create_ShouldReturnInvalidLatitudeError_WhenLatitudeIsAbove90()
    {
        var result = Coordinates.Create(90.001, 0);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Coordinates.InvalidLatitude");
    }

    #endregion

    #region Create - invalid longitude

    [Fact(DisplayName = "Create returns InvalidLongitude error when longitude is below -180")]
    public void Create_ShouldReturnInvalidLongitudeError_WhenLongitudeIsBelowNegative180()
    {
        var result = Coordinates.Create(0, -180.001);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Coordinates.InvalidLongitude");
    }

    [Fact(DisplayName = "Create returns InvalidLongitude error when longitude is above 180")]
    public void Create_ShouldReturnInvalidLongitudeError_WhenLongitudeIsAbove180()
    {
        var result = Coordinates.Create(0, 180.001);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Coordinates.InvalidLongitude");
    }

    [Fact(DisplayName = "Create returns InvalidLatitude error first when both latitude and longitude are invalid")]
    public void Create_ShouldReturnInvalidLatitudeError_WhenBothValuesAreInvalid()
    {
        var result = Coordinates.Create(91, 181);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Coordinates.InvalidLatitude");
    }

    #endregion

    #region ToString

    [Fact(DisplayName = "ToString returns 'Latitude, Longitude' format")]
    public void ToString_ShouldReturnLatitudeLongitudeFormat()
    {
        var result = Coordinates.Create(41.7508, -72.6850);

        result.Value.ToString().ShouldBe("41.7508, -72.685");
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two Coordinates with the same values are equal")]
    public void Equality_ShouldBeEqual_WhenValuesAreTheSame()
    {
        var a = Coordinates.Create(41.7508, -72.6850);
        var b = Coordinates.Create(41.7508, -72.6850);

        a.Value.ShouldBe(b.Value);
    }

    [Fact(DisplayName = "Two Coordinates with different values are not equal")]
    public void Equality_ShouldNotBeEqual_WhenValuesAreDifferent()
    {
        var a = Coordinates.Create(41.7508, -72.6850);
        var b = Coordinates.Create(42.3601, -71.0589);

        a.Value.ShouldNotBe(b.Value);
    }

    #endregion
}