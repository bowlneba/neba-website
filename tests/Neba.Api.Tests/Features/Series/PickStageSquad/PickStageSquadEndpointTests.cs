using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.PickStageSquad;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.PickStageSquad;

[UnitTest]
[Component("Series")]
public sealed class PickStageSquadEndpointTests
{
    private static Mock<NebaMessaging.ICommandHandler<PickStageSquadCommand, Updated>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register a PUT route at /series/{id}/stages/{stageId}/squad that needs the Series.EditSeries permission")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<PickStageSquadEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/stages/{stageId}/squad");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.EditSeries");
        endpoint.Definition.PreBuiltUserPolicies!.Count.ShouldBe(1);
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map the request to the command and return 204")]
    public async Task HandleAsync_ShouldMapRequestToCommand_AndReturn204()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = PickStageSquadRequestFactory.Bogus(1, 202).Single();
        PickStageSquadCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<PickStageSquadCommand>(), ct))
            .Callback<PickStageSquadCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Result.Updated);
        var endpoint = Factory.Create<PickStageSquadEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(request.Id));
        captured.StageId.ShouldBe(new SeriesStageId(request.StageId));
        captured.SquadId.ShouldBe(new SquadId(request.Squad.SquadId));
        captured.Version.ShouldBe(request.Squad.Version);
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
        handler.Setup(h => h.HandleAsync(It.IsAny<PickStageSquadCommand>(), ct)).ReturnsAsync(error);
        var endpoint = Factory.Create<PickStageSquadEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(PickStageSquadRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expected);
    }

    [Fact(DisplayName = "HandleAsync should return 422 for a squad that is not in the Stage's tournament")]
    public async Task HandleAsync_ShouldReturn422_WhenSquadIsNotInTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<PickStageSquadCommand>(), ct)).ReturnsAsync(SeriesAdvancementErrors.SquadNotInTournament);
        var endpoint = Factory.Create<PickStageSquadEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(PickStageSquadRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
        endpoint.ValidationFailures.Single().ErrorCode.ShouldBe("Series.Squad.NotInTournament");
    }
}