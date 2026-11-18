using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Features.Series.CreateSeries;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.CreateSeries;

[UnitTest]
[Component("Series")]
public sealed class CreateSeriesEndpointTests
{
    private static Mock<NebaMessaging.ICommandHandler<CreateSeriesCommand, SeriesId>> Handler()
        => new(MockBehavior.Strict);

    private static CreateSeriesRequest Request()
        => new()
        {
            Series = SeriesInputFactory.Create(
                seasonId: new string('2', 26),
                logo: SeriesLogoInputFactory.Create(),
                stages: [SeriesStageInputFactory.Create(kind: "Survivor", tournamentId: new string('1', 26), advancingCount: 20,
                    squadDate: new DateOnly(2025, 7, 12), squadStartTime: new TimeOnly(9, 0))],
                finale: SeriesFinaleInputFactory.Create(bowlingCenterId: "01948")),
        };

    // Send.CreatedAtAsync needs a LinkGenerator that Factory.Create does not provide, so the 201 path is
    // covered by SeriesEndpointsTests over real HTTP.

    [Fact(DisplayName = "Configure should register a permission-protected POST route at /series")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<CreateSeriesEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain("/series/");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.CreateSeries");
    }

    [Fact(DisplayName = "HandleAsync should map the request to the command and return 422 for a validation error")]
    public async Task HandleAsync_ShouldMapCommand_AndReturn422_ForValidationError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        CreateSeriesCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<CreateSeriesCommand>(), ct))
            .Callback<CreateSeriesCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(SeriesErrors.SeasonNotFound("x"));
        var endpoint = Factory.Create<CreateSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
        captured!.Name.ShouldBe(SeriesInputFactory.ValidName);
        captured.SeasonId.Value.ToString().ShouldBe(new string('2', 26));
        captured.Logo!.Path.ShouldBe(SeriesLogoInputFactory.ValidPath);
        captured.Stages.Single().Kind.ShouldBe(StageKind.Survivor);
        captured.Finale!.BowlingCenterId!.Value.ShouldBe("01948");
    }

    [Fact(DisplayName = "HandleAsync should return 409 when every error is a Conflict")]
    public async Task HandleAsync_ShouldReturn409_ForConflict()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<CreateSeriesCommand>(), ct))
            .ReturnsAsync(SeriesErrors.TournamentInAnotherSeries(1, "T", "Other"));
        var endpoint = Factory.Create<CreateSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }
}