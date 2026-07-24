using FastEndpoints;

using Neba.Api.Features.Tournaments.CreateOilPattern;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.OilPatterns;
using Neba.TestFactory.Tournaments;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.CreateOilPattern;

[UnitTest]
[Component("Tournaments")]
public sealed class CreateOilPatternEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map request fields to command and return 200 with mapped response when creation succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndReturnMappedResponse_WhenCreationSucceeds()
    {
        // Arrange
        var request = CreateOilPatternRequestFactory.Create(
            name: "Dragon",
            length: 40,
            volume: 24.5m,
            leftRatio: 5.5m,
            rightRatio: 8.0m,
            kegelId: Guid.NewGuid());
        var ct = TestContext.Current.CancellationToken;
        var createdOilPattern = CreatedOilPatternFactory.Create(name: "Dragon", length: 40);

        CreateOilPatternCommand? capturedCommand = null;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateOilPatternCommand, CreatedOilPattern>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateOilPatternCommand>(), ct))
            .Callback<CreateOilPatternCommand, CancellationToken>((c, _) => capturedCommand = c)
            .ReturnsAsync(createdOilPattern);

        var endpoint = Factory.Create<CreateOilPatternEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        capturedCommand.ShouldNotBeNull();
        capturedCommand.Name.ShouldBe(request.OilPattern.Name);
        capturedCommand.Length.ShouldBe(request.OilPattern.Length);
        capturedCommand.Volume.ShouldBe(request.OilPattern.Volume);
        capturedCommand.LeftRatio.ShouldBe(request.OilPattern.LeftRatio);
        capturedCommand.RightRatio.ShouldBe(request.OilPattern.RightRatio);
        capturedCommand.KegelId.ShouldBe(request.OilPattern.KegelId);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.OilPatternId.ShouldBe(createdOilPattern.Id.Value.ToString());
        endpoint.Response.Name.ShouldBe(createdOilPattern.Name);
        endpoint.Response.Length.ShouldBe(createdOilPattern.Length);
        endpoint.Response.LengthCategory.ShouldBe(createdOilPattern.LengthCategory.Name);
        endpoint.Response.RatioCategory.ShouldBe(createdOilPattern.RatioCategory.Name);
    }

    [Fact(DisplayName = "HandleAsync should return 409 Conflict when the command returns a conflict error")]
    public async Task HandleAsync_ShouldReturn409_WhenCommandReturnsConflictError()
    {
        // Arrange
        var request = CreateOilPatternRequestFactory.Create(kegelId: Guid.NewGuid());
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateOilPatternCommand, CreatedOilPattern>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateOilPatternCommand>(), ct))
            .ReturnsAsync(OilPatternErrors.KegelIdAlreadyExists(request.OilPattern.KegelId!.Value));

        var endpoint = Factory.Create<CreateOilPatternEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = CreateOilPatternRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateOilPatternCommand, CreatedOilPattern>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateOilPatternCommand>(), ct))
            .ReturnsAsync(OilPatternErrors.NameRequired);

        var endpoint = Factory.Create<CreateOilPatternEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /oil-patterns")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderOilPatternsPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateOilPatternCommand, CreatedOilPattern>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateOilPatternEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("oil-patterns"), "should be under the /oil-patterns path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}