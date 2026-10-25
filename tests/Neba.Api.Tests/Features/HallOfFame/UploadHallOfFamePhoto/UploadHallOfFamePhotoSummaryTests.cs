using Neba.Api.Features.HallOfFame.UploadHallOfFamePhoto;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.UploadHallOfFamePhoto;

[UnitTest]
[Component("HallOfFame")]
public sealed class UploadHallOfFamePhotoSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new UploadHallOfFamePhotoSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new UploadHallOfFamePhotoSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 200, 400, 401, 403, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new UploadHallOfFamePhotoSummary();

        // Assert
        summary.Responses.ShouldContainKey(200);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(422);
    }
}