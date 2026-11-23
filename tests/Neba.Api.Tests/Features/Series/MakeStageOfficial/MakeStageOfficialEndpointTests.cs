using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.MakeStageOfficial;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.MakeStageOfficial;

[UnitTest]
[Component("Series")]
public sealed class MakeStageOfficialEndpointTests
{
    private static Mock<NebaMessaging.ICommandHandler<MakeStageOfficialCommand, Updated>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register a PUT route at /series/{id}/stages/{stageId}/official that needs the Series.MakeStageOfficial permission")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<MakeStageOfficialEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/stages/{stageId}/official");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.MakeStageOfficial");
        endpoint.Definition.PreBuiltUserPolicies!.Count.ShouldBe(1);
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map the request to the command and return 204")]
    public async Task HandleAsync_ShouldMapRequestToCommand_AndReturn204()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = MakeStageOfficialRequestFactory.Bogus(1, 303).Single();
        MakeStageOfficialCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<MakeStageOfficialCommand>(), ct))
            .Callback<MakeStageOfficialCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Result.Updated);
        var endpoint = Factory.Create<MakeStageOfficialEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(request.Id));
        captured.StageId.ShouldBe(new SeriesStageId(request.StageId));
        captured.Version.ShouldBe(request.Official.Version);
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
        handler.Setup(h => h.HandleAsync(It.IsAny<MakeStageOfficialCommand>(), ct)).ReturnsAsync(error);
        var endpoint = Factory.Create<MakeStageOfficialEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(MakeStageOfficialRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expected);
    }
}
