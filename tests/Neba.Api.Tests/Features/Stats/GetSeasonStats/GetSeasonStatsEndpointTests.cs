using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.Application.Messaging;
using Neba.Application.Stats.GetSeasonStats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats;

namespace Neba.Api.Tests.Features.Stats.GetSeasonStats;

[UnitTest]
[Component("Stats")]
public sealed class GetSeasonStatsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped season stats when query succeeds without year")]
    public async Task HandleAsync_ShouldReturnOkWithMappedSeasonStats_WhenQuerySucceedsWithoutYear()
    {
        // Arrange
        var dto = SeasonStatsDtoFactory.Bogus(count: 1, seed: 100).Single();
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetSeasonStatsQuery>(), cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetSeasonStatsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetSeasonStatsRequest { Year = null }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.SelectedSeason.ShouldBe(dto.Season.Description);
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return OK with mapped season stats when query succeeds with year")]
    public async Task HandleAsync_ShouldReturnOkWithMappedSeasonStats_WhenQuerySucceedsWithYear()
    {
        // Arrange
        var dto = SeasonStatsDtoFactory.Bogus(count: 1, seed: 200).Single();
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.Is<GetSeasonStatsQuery>(q => q.SeasonYear == 2024), cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetSeasonStatsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetSeasonStatsRequest { Year = 2024 }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return 404 Not Found when no stats exist for the season")]
    public async Task HandleAsync_ShouldReturn404_WhenNoStatsExistForSeason()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetSeasonStatsQuery>(), cancellationToken))
            .ReturnsAsync(Error.NotFound());

        var endpoint = Factory.Create<GetSeasonStatsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetSeasonStatsRequest { Year = 1999 }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 500 when query returns a null success payload")]
    public async Task HandleAsync_ShouldReturn500_WhenQueryReturnsNullPayload()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetSeasonStatsQuery>(), cancellationToken))
            .ReturnsAsync(default(ErrorOr<SeasonStatsDto>));

        var endpoint = Factory.Create<GetSeasonStatsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetSeasonStatsRequest { Year = null }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
        endpoint.ValidationFailures.ShouldContain(f => f.ErrorMessage == "Season stats payload was null.");
    }

    [Fact(DisplayName = "Configure should register anonymous GET route under /stats path")]
    public void Configure_ShouldRegisterAnonymousGetRoute_UnderStatsPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetSeasonStatsEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("stats"), "should be under the /stats path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}