using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.HallOfFame.EditHallOfFamePhoto;

[UnitTest]
[Component("HallOfFame")]
public sealed class EditHallOfFamePhotoEndpointTests
{
    private static (Mock<NebaMessaging.ICommandHandler<EditHallOfFamePhotoCommand, Updated>> Handler, Func<EditHallOfFamePhotoCommand?> Captured) CreateHandlerMock(
        CancellationToken ct,
        ErrorOr<Updated>? result = null)
    {
        EditHallOfFamePhotoCommand? captured = null;

        var mock = new Mock<NebaMessaging.ICommandHandler<EditHallOfFamePhotoCommand, Updated>>(MockBehavior.Strict);
        mock
            .Setup(h => h.HandleAsync(It.IsAny<EditHallOfFamePhotoCommand>(), ct))
            .Callback<EditHallOfFamePhotoCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(result ?? Result.Updated);

        return (mock, () => captured);
    }

    [Fact(DisplayName = "HandleAsync should map the request to the command and return 204 when the edit succeeds")]
    public async Task HandleAsync_ShouldMapRequestAndReturn204_WhenEditSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var inductionId = HallOfFameId.New();
        var photo = EditHallOfFamePhotoRequestFactory.CreatePhoto(path: "hall-of-fame/photo/new.png", sizeInBytes: 4096);
        var request = EditHallOfFamePhotoRequestFactory.Create(inductionId.Value.ToString(), photo, focalX: 31, focalY: 36, zoom: 175);
        var (handler, captured) = CreateHandlerMock(ct);
        var endpoint = Factory.Create<EditHallOfFamePhotoEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        var command = captured().ShouldNotBeNull();
        command.HallOfFameId.ShouldBe(inductionId);
        command.Photo.ShouldNotBeNull();
        command.Photo.Container.ShouldBe(photo.Container);
        command.Photo.Path.ShouldBe(photo.Path);
        command.Photo.ContentType.ShouldBe(photo.ContentType);
        command.Photo.SizeInBytes.ShouldBe(photo.SizeInBytes);
        command.FocalX.ShouldBe(31);
        command.FocalY.ShouldBe(36);
        command.Zoom.ShouldBe(175);
    }

    [Fact(DisplayName = "HandleAsync should send a null photo and no framing when the request removes the photo")]
    public async Task HandleAsync_ShouldSendNullPhotoAndNoFraming_WhenRequestRemovesPhoto()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = EditHallOfFamePhotoRequestFactory.Create(HallOfFameId.New().Value.ToString());
        var (handler, captured) = CreateHandlerMock(ct);
        var endpoint = Factory.Create<EditHallOfFamePhotoEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        var command = captured().ShouldNotBeNull();
        command.Photo.ShouldBeNull();
        command.FocalX.ShouldBeNull();
        command.FocalY.ShouldBeNull();
        command.Zoom.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should send a null photo with the framing when the request only changes the framing")]
    public async Task HandleAsync_ShouldSendNullPhotoWithFraming_WhenRequestOnlyChangesFraming()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = EditHallOfFamePhotoRequestFactory.Create(HallOfFameId.New().Value.ToString(), photo: null, focalX: 10, focalY: 90, zoom: 200);
        var (handler, captured) = CreateHandlerMock(ct);
        var endpoint = Factory.Create<EditHallOfFamePhotoEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        var command = captured().ShouldNotBeNull();
        command.Photo.ShouldBeNull();
        command.FocalX.ShouldBe(10);
        command.FocalY.ShouldBe(90);
        command.Zoom.ShouldBe(200);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the command returns a not-found error")]
    public async Task HandleAsync_ShouldReturn404_WhenCommandReturnsNotFoundError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (handler, _) = CreateHandlerMock(ct, HallOfFameErrors.InductionNotFound(HallOfFameId.New()));
        var endpoint = Factory.Create<EditHallOfFamePhotoEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(EditHallOfFamePhotoRequestFactory.Create(HallOfFameId.New().Value.ToString()), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 409 when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (handler, _) = CreateHandlerMock(ct, HallOfFameErrors.NoPhoto);
        var endpoint = Factory.Create<EditHallOfFamePhotoEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(EditHallOfFamePhotoRequestFactory.Create(HallOfFameId.New().Value.ToString(), focalX: 1, focalY: 2, zoom: 100), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns a validation error")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (handler, _) = CreateHandlerMock(ct, HallOfFameErrors.PhotoNotUploaded);
        var endpoint = Factory.Create<EditHallOfFamePhotoEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(EditHallOfFamePhotoRequestFactory.Create(HallOfFameId.New().Value.ToString()), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected PUT route under /hall-of-fame")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute_UnderHallOfFamePath()
    {
        // Arrange
        var handler = new Mock<NebaMessaging.ICommandHandler<EditHallOfFamePhotoCommand, Updated>>(MockBehavior.Strict);
        var endpoint = Factory.Create<EditHallOfFamePhotoEndpoint>(handler.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain("/hall-of-fame/{id}/photo");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
