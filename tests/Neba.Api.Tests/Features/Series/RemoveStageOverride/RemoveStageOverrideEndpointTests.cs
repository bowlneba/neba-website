using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.RemoveStageOverride;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.RemoveStageOverride;

[UnitTest]
[Component("Series")]
public sealed class RemoveStageOverrideEndpointTests
{
    private static Mock<NebaMessaging.ICommandHandler<RemoveStageOverrideCommand, Deleted>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register a DELETE route at /series/{id}/stages/{stageId}/overrides/{overrideId} that needs the Series.ManageAdvancement permission")]
    public void Configure_ShouldRegisterPermissionProtectedDeleteRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<RemoveStageOverrideEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("DELETE");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/stages/{stageId}/overrides/{overrideId}");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.ManageAdvancement");
        endpoint.Definition.PreBuiltUserPolicies!.Count.ShouldBe(1);
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map the request to the command and return 204")]
    public async Task HandleAsync_ShouldMapRequestToCommand_AndReturn204()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = RemoveStageOverrideRequestFactory.Bogus(1, 203).Single();
        RemoveStageOverrideCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<RemoveStageOverrideCommand>(), ct))
            .Callback<RemoveStageOverrideCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Result.Deleted);
        var endpoint = Factory.Create<RemoveStageOverrideEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(request.Id));
        captured.StageId.ShouldBe(new SeriesStageId(request.StageId));
        captured.OverrideId.ShouldBe(new StageOverrideId(request.OverrideId));
        captured.Version.ShouldBe(request.Version!.Value);
        await Verify(captured);
    }

    [Theory(DisplayName = "HandleAsync should map NotFound, Conflict and Validation errors to 404, 409 and 422")]
    [InlineData("NotFound", 404)]
    [InlineData("Conflict", 409)]
    [InlineData("Validation", 422)]
    public async Task HandleAsync_ShouldMapErrorTypesToStatusCodes(string type, int expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var error = type switch
        {
            "NotFound" => Error.NotFound("Series.NotFound", "Not found."),
            "Conflict" => Error.Conflict("Series.Conflict", "Conflict."),
            _ => Error.Validation("Series.Validation", "Invalid."),
        };
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<RemoveStageOverrideCommand>(), ct)).ReturnsAsync(error);
        var endpoint = Factory.Create<RemoveStageOverrideEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(RemoveStageOverrideRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expected);
    }

    [Fact(DisplayName = "HandleAsync should return 409 for an official Stage")]
    public async Task HandleAsync_ShouldReturn409_ForOfficialStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<RemoveStageOverrideCommand>(), ct))
            .ReturnsAsync(SeriesAdvancementErrors.StageNotComputed(StageStatus.Official));
        var endpoint = Factory.Create<RemoveStageOverrideEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(RemoveStageOverrideRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
        endpoint.ValidationFailures.Single().ErrorCode.ShouldBe("Series.Override.StageNotComputed");
    }
}