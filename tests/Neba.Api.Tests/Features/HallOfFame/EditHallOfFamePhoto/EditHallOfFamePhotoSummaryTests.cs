using Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.EditHallOfFamePhoto;

[UnitTest]
[Component("HallOfFame")]
public sealed class EditHallOfFamePhotoSummaryTests
{
    [Fact(DisplayName = "Constructor should set Summary description")]
    public void Constructor_ShouldSetSummaryDescription()
    {
        // Arrange & Act
        var summary = new EditHallOfFamePhotoSummary();

        // Assert
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should set Description")]
    public void Constructor_ShouldSetDescription()
    {
        // Arrange & Act
        var summary = new EditHallOfFamePhotoSummary();

        // Assert
        summary.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Constructor should register 204, 400, 401, 403, 404, 409, and 422 responses")]
    public void Constructor_ShouldRegisterExpectedResponses()
    {
        // Arrange & Act
        var summary = new EditHallOfFamePhotoSummary();

        // Assert
        summary.Responses.ShouldContainKey(204);
        summary.Responses.ShouldContainKey(400);
        summary.Responses.ShouldContainKey(401);
        summary.Responses.ShouldContainKey(403);
        summary.Responses.ShouldContainKey(404);
        summary.Responses.ShouldContainKey(409);
        summary.Responses.ShouldContainKey(422);
    }

    [Fact(DisplayName = "Constructor should set an example request with a photo and framing")]
    public void Constructor_ShouldSetExampleRequest()
    {
        // Arrange & Act
        var summary = new EditHallOfFamePhotoSummary();

        // Assert
        var example = summary.ExampleRequest.ShouldBeOfType<Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto.EditHallOfFamePhotoRequest>();
        example.Id.Length.ShouldBe(26);
        example.Input.Photo.ShouldNotBeNull();
        example.Input.FocalX.ShouldNotBeNull();
        example.Input.FocalY.ShouldNotBeNull();
        example.Input.Zoom.ShouldNotBeNull();
    }
}