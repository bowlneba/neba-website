using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeries;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.GetSeries;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesEndpointTests
{
    private const string ValidId = "01000000000000000000000001";

    private static Mock<IQueryHandler<GetSeriesQuery, ErrorOr<SeriesDetailDto>>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register an anonymous GET route at /series/{id}")]
    public void Configure_ShouldRegisterAnonymousGetRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<GetSeriesEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/series/{id}");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with the Series, Finale and Stages mapped")]
    public async Task HandleAsync_ShouldMapDetail()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var stage = SeriesStageDtoFactory.Create(kind: "Survivor", squadDate: new DateOnly(2025, 7, 12), squadStartTime: new TimeOnly(9, 0), teamSize: 3);
        var dto = SeriesDetailDtoFactory.Create(
            name: "VH",
            isLocked: true,
            finale: SeriesFinaleDtoFactory.Create(bowlingCenterName: "Callahan's"),
            logoUrl: new Uri("https://example.com/l.png"),
            logoContainer: "series",
            logoPath: "l.png",
            logoContentType: "image/png",
            logoSizeInBytes: 5,
            stages: [stage]);

        var handler = Handler();
        GetSeriesQuery? captured = null;
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesQuery>(), ct))
            .Callback<GetSeriesQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(dto);
        var endpoint = Factory.Create<GetSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesRequest { SeriesId = ValidId }, ct);

        // Assert
        captured!.Id.ShouldBe(new SeriesId(ValidId));
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var response = endpoint.Response;
        response.Id.ShouldBe(dto.Id.Value.ToString());
        response.Name.ShouldBe("VH");
        response.IsLocked.ShouldBeTrue();
        response.Finale!.BowlingCenterName.ShouldBe("Callahan's");
        response.LogoUrl.ShouldBe(new Uri("https://example.com/l.png"));
        response.LogoContainer.ShouldBe("series");
        response.LogoPath.ShouldBe("l.png");
        response.LogoContentType.ShouldBe("image/png");
        response.LogoSizeInBytes.ShouldBe(5);
        var mapped = response.Stages.Single();
        mapped.Id.ShouldBe(stage.Id.Value.ToString());
        mapped.Kind.ShouldBe("Survivor");
        mapped.SquadDate.ShouldBe(new DateOnly(2025, 7, 12));
        mapped.SquadStartTime.ShouldBe(new TimeOnly(9, 0));
        mapped.TournamentId.ShouldBe(stage.TournamentId.Value.ToString());
        mapped.TeamSize.ShouldBe(3);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the Series does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesQuery>(), ct)).ReturnsAsync(SeriesErrors.NotFound(new SeriesId(ValidId)));
        var endpoint = Factory.Create<GetSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 500 when the query fails with another error")]
    public async Task HandleAsync_ShouldReturn500_WhenQueryFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesQuery>(), ct)).ReturnsAsync(Error.Failure("Series.Boom", "boom"));
        var endpoint = Factory.Create<GetSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }
}