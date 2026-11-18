using FastEndpoints;

using Neba.Api.Features.Series.ListSeries;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.ListSeries;

[UnitTest]
[Component("Series")]
public sealed class ListSeriesEndpointTests
{
    [Fact(DisplayName = "Configure should register an anonymous GET route at /series (empty route under the group)")]
    public void Configure_ShouldRegisterAnonymousGetRoute()
    {
        // Arrange
        var handler = new Mock<IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>>>(MockBehavior.Strict);

        // Act
        var endpoint = Factory.Create<ListSeriesEndpoint>(handler.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/series/");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with each Series and its next Stage mapped")]
    public async Task HandleAsync_ShouldMapSeriesAndNextStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var next = SeriesNextStageDtoFactory.Create(kind: "Survivor", tournamentName: "Lang's");
        var withNext = SeriesSummaryDtoFactory.Create(name: "With", stageTournamentIds: [TournamentId.New(), TournamentId.New()], nextStage: next, logoUrl: new Uri("https://example.com/l.png"), isLocked: true);
        var withoutNext = SeriesSummaryDtoFactory.Create(name: "Without");

        var handler = new Mock<IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>>>(MockBehavior.Strict);
        handler.Setup(h => h.HandleAsync(It.IsAny<ListSeriesQuery>(), ct)).ReturnsAsync([withNext, withoutNext]);
        var endpoint = Factory.Create<ListSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Items.Select(i => i.Name).ShouldBe(["With", "Without"]);
        var first = endpoint.Response.Items.First();
        first.Id.ShouldBe(withNext.Id.Value.ToString());
        first.SeasonId.ShouldBe(withNext.SeasonId.Value.ToString());
        first.IsLocked.ShouldBeTrue();
        first.LogoUrl.ShouldBe(new Uri("https://example.com/l.png"));
        first.QualifyingStageCount.ShouldBe(withNext.QualifyingStageCount);
        first.StageTournamentIds.ShouldBe([.. withNext.StageTournamentIds.Select(id => id.Value.ToString())]);
        first.NextStage.ShouldNotBeNull();
        first.NextStage.Kind.ShouldBe("Survivor");
        first.NextStage.TournamentId.ShouldBe(next.TournamentId.Value.ToString());
        first.NextStage.TournamentName.ShouldBe("Lang's");
        endpoint.Response.Items.Last().NextStage.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return OK with an empty collection when there are no Series")]
    public async Task HandleAsync_ShouldReturnEmptyCollection()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new Mock<IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>>>(MockBehavior.Strict);
        handler.Setup(h => h.HandleAsync(It.IsAny<ListSeriesQuery>(), ct)).ReturnsAsync([]);
        var endpoint = Factory.Create<ListSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}