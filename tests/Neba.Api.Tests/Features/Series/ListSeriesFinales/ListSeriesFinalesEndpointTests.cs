using FastEndpoints;

using Neba.Api.Features.Series.ListSeriesFinales;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.ListSeriesFinales;

[UnitTest]
[Component("Series")]
public sealed class ListSeriesFinalesEndpointTests
{
    [Fact(DisplayName = "Configure should register an anonymous GET route at /series/finales")]
    public void Configure_ShouldRegisterAnonymousGetRoute()
    {
        // Arrange
        var handler = new Mock<IQueryHandler<ListSeriesFinalesQuery, IReadOnlyCollection<SeriesFinaleSummaryDto>>>(MockBehavior.Strict);

        // Act
        var endpoint = Factory.Create<ListSeriesFinalesEndpoint>(handler.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/series/finales");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with each Finale and its host mapped, in handler order")]
    public async Task HandleAsync_ShouldMapFinales_InHandlerOrder()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var hosted = SeriesFinaleSummaryDtoFactory.Create(
            seriesName: "Hosted",
            logoUrl: new Uri("https://example.com/l.png"),
            finale: SeriesFinaleDtoFactory.Create(bowlingCenterId: "01948", bowlingCenterName: "Callahan's", bowlingCenterCity: "Seekonk", bowlingCenterState: "MA"));
        var unhosted = SeriesFinaleSummaryDtoFactory.Create(seriesName: "Unhosted");

        var handler = new Mock<IQueryHandler<ListSeriesFinalesQuery, IReadOnlyCollection<SeriesFinaleSummaryDto>>>(MockBehavior.Strict);
        handler.Setup(h => h.HandleAsync(It.IsAny<ListSeriesFinalesQuery>(), ct)).ReturnsAsync([hosted, unhosted]);
        var endpoint = Factory.Create<ListSeriesFinalesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Items.Select(i => i.SeriesName).ShouldBe(["Hosted", "Unhosted"]);
        var first = endpoint.Response.Items.First();
        first.SeriesId.ShouldBe(hosted.SeriesId.Value.ToString());
        first.SeasonId.ShouldBe(hosted.SeasonId.Value.ToString());
        first.LogoUrl.ShouldBe(new Uri("https://example.com/l.png"));
        first.Finale.FinalistCount.ShouldBe(4);
        first.Finale.BowlingCenterName.ShouldBe("Callahan's");
        first.Finale.BowlingCenterCity.ShouldBe("Seekonk");
        first.Finale.BowlingCenterState.ShouldBe("MA");
        endpoint.Response.Items.Last().Finale.BowlingCenterId.ShouldBeNull();
    }
}