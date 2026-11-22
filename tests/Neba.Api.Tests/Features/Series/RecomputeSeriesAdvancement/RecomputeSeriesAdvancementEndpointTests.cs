using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.RecomputeSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.RecomputeSeriesAdvancement;

[UnitTest]
[Component("Series")]
public sealed class RecomputeSeriesAdvancementEndpointTests
{
    private static Mock<NebaMessaging.ICommandHandler<RecomputeSeriesAdvancementCommand, Updated>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register a POST route at /series/{id}/stages/{stageId}/recomputations that needs the Series.EditSeries permission")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<RecomputeSeriesAdvancementEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/stages/{stageId}/recomputations");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.EditSeries");
        endpoint.Definition.PreBuiltUserPolicies!.Count.ShouldBe(1);
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map the request to the command and return 204")]
    public async Task HandleAsync_ShouldMapRequestToCommand_AndReturn204()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = RecomputeSeriesAdvancementRequestFactory.Bogus(1, 201).Single();
        RecomputeSeriesAdvancementCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<RecomputeSeriesAdvancementCommand>(), ct))
            .Callback<RecomputeSeriesAdvancementCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Result.Updated);
        var endpoint = Factory.Create<RecomputeSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(request.Id));
        captured.StageId.ShouldBe(new SeriesStageId(request.StageId));
        captured.Version.ShouldBe(request.Recomputation.Version);
        captured.ConfirmResetOfficial.ShouldBe(request.Recomputation.ConfirmResetOfficial);
        await Verify(captured);
    }

    [Fact(DisplayName = "HandleAsync should send ConfirmResetOfficial true to the command when the request confirms")]
    public async Task HandleAsync_ShouldPassConfirmation_WhenRequestConfirms()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = RecomputeSeriesAdvancementRequestFactory.Create(
            recomputation: RecomputeSeriesAdvancementInputFactory.Create(version: 9, confirmResetOfficial: true));
        RecomputeSeriesAdvancementCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<RecomputeSeriesAdvancementCommand>(), ct))
            .Callback<RecomputeSeriesAdvancementCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Result.Updated);
        var endpoint = Factory.Create<RecomputeSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        captured!.ConfirmResetOfficial.ShouldBeTrue();
        captured.Version.ShouldBe(9);
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
        handler.Setup(h => h.HandleAsync(It.IsAny<RecomputeSeriesAdvancementCommand>(), ct)).ReturnsAsync(error);
        var endpoint = Factory.Create<RecomputeSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(RecomputeSeriesAdvancementRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expected);
    }

    [Fact(DisplayName = "HandleAsync should return 409 with one Series.Stages[n] failure per official Stage when confirmation is required")]
    public async Task HandleAsync_ShouldReturn409WithOneFailurePerOfficialStage_WhenConfirmationRequired()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<RecomputeSeriesAdvancementCommand>(), ct))
            .ReturnsAsync(SeriesAdvancementErrors.ConfirmationRequired([1, 2]));
        var endpoint = Factory.Create<RecomputeSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(RecomputeSeriesAdvancementRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
        endpoint.ValidationFailures.Select(f => f.PropertyName).ShouldBe(["Series.Stages[0]", "Series.Stages[1]"]);
    }
}