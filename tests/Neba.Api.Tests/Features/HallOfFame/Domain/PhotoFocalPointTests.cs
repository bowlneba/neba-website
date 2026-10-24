using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.Domain;

[UnitTest]
[Component("HallOfFame")]
public sealed class PhotoFocalPointTests
{
    [Theory(DisplayName = "Create should succeed at the lowest and highest allowed values")]
    [InlineData(0, 0, 100)]
    [InlineData(100, 100, 300)]
    [InlineData(50, 50, 100)]
    public void Create_ShouldSucceed_WhenValuesAreWithinRange(int x, int y, int zoom)
    {
        // Arrange
        // Act
        var result = PhotoFocalPoint.Create(x, y, zoom);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.X.ShouldBe(x);
        result.Value.Y.ShouldBe(y);
        result.Value.Zoom.ShouldBe(zoom);
    }

    [Theory(DisplayName = "Create should return a validation error when a value is out of range")]
    [InlineData(-1, 50, 100)]
    [InlineData(101, 50, 100)]
    [InlineData(50, -1, 100)]
    [InlineData(50, 101, 100)]
    [InlineData(50, 50, 99)]
    [InlineData(50, 50, 301)]
    public void Create_ShouldReturnValidationError_WhenValueIsOutOfRange(int x, int y, int zoom)
    {
        // Arrange
        // Act
        var result = PhotoFocalPoint.Create(x, y, zoom);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
        result.FirstError.Code.ShouldBe("HallOfFame.Photo.FocalPointOutOfRange");
    }

    [Fact(DisplayName = "Default should be the center of the photo with no zoom")]
    public void Default_ShouldBeCenteredWithNoZoom()
    {
        // Arrange
        // Act
        var point = PhotoFocalPoint.Default;

        // Assert
        point.X.ShouldBe(50);
        point.Y.ShouldBe(50);
        point.Zoom.ShouldBe(100);
    }
}
