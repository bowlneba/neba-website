using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesMappingExtensionsTests
{
    [Fact(DisplayName = "Summary should map the season year, range and counts")]
    public void SummaryToViewModel_ShouldMapSeason()
    {
        // Arrange
        var response = SeriesSummaryResponseFactory.Create();

        // Act
        var model = response.ToViewModel();

        // Assert
        model.Id.ShouldBe(response.Id);
        model.SeasonYear.ShouldBe(2025);
        model.SeasonEndDate.ShouldBe(response.SeasonEndDate);
        model.SeasonDateRange.ShouldBe("1/1/2025 to 12/31/2025");
        model.QualifyingStageCount.ShouldBe(8);
        model.NextStage.ShouldBeNull();
        model.LogoUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "Summary should map the next Stage and the logo when present")]
    public void SummaryToViewModel_ShouldMapNextStageAndLogo()
    {
        // Arrange
        var logo = new Uri("https://storage.example.com/series/logo.png");
        var response = SeriesSummaryResponseFactory.Create(
            logoUrl: logo,
            nextStage: SeriesNextStageResponseFactory.Create());

        // Act
        var model = response.ToViewModel();

        // Assert
        model.LogoUrl.ShouldBe(logo);
        var next = model.NextStage.ShouldNotBeNull();
        next.Kind.ShouldBe(response.NextStage!.Kind);
        next.TournamentName.ShouldBe(response.NextStage.TournamentName);
        next.Dates.ShouldBe(SeriesDisplay.TournamentDates(response.NextStage.TournamentStartDate, response.NextStage.TournamentEndDate));
    }

    [Fact(DisplayName = "Detail should sort Stages by order")]
    public void DetailToViewModel_ShouldSortStagesByOrder()
    {
        // Arrange
        var response = SeriesDetailResponseFactory.Create(stages:
        [
            SeriesStageResponseFactory.Create(id: "c", order: 3),
            SeriesStageResponseFactory.Create(id: "a", order: 1),
            SeriesStageResponseFactory.Create(id: "b", order: 2),
        ]);

        // Act
        var model = response.ToViewModel();

        // Assert
        model.Stages.Select(s => s.Id).ShouldBe(["a", "b", "c"]);
    }

    [Fact(DisplayName = "Detail should have no Finale and no logo input when the API sends none")]
    public void DetailToViewModel_ShouldHaveNoFinaleOrLogo()
    {
        // Arrange
        var response = SeriesDetailResponseFactory.Create();

        // Act
        var model = response.ToViewModel();

        // Assert
        model.Finale.ShouldBeNull();
        model.HasFinale.ShouldBeFalse();
        model.Logo.ShouldBeNull();
    }

    [Fact(DisplayName = "Detail should map the Finale with its host text")]
    public void DetailToViewModel_ShouldMapFinaleHost()
    {
        // Arrange
        var response = SeriesDetailResponseFactory.Create(finale: SeriesFinaleResponseFactory.Create(
            bowlingCenterId: "0123",
            bowlingCenterName: "Callahan's Bowl-O-Rama",
            bowlingCenterCity: "Seekonk",
            bowlingCenterState: "MA"));

        // Act
        var model = response.ToViewModel();

        // Assert
        model.HasFinale.ShouldBeTrue();
        var finale = model.Finale.ShouldNotBeNull();
        finale.FinalistCount.ShouldBe(4);
        finale.BowlingCenterId.ShouldBe("0123");
        finale.Host.ShouldBe("Callahan's Bowl-O-Rama, Seekonk, MA");
    }

    [Fact(DisplayName = "Detail should have a null host when the Finale has no center")]
    public void DetailToViewModel_ShouldHaveNullHost_WhenNoCenter()
    {
        // Arrange
        var response = SeriesDetailResponseFactory.Create(finale: SeriesFinaleResponseFactory.Create());

        // Act
        var model = response.ToViewModel();

        // Assert
        model.Finale.ShouldNotBeNull().Host.ShouldBeNull();
    }

    [Fact(DisplayName = "Detail should build the logo input only when container and path are present")]
    public void DetailToViewModel_ShouldBuildLogoInput_OnlyWithContainerAndPath()
    {
        // Arrange
        var full = SeriesDetailResponseFactory.Create(
            logoContainer: "bowlneba-public",
            logoPath: "series/logo/a.png",
            logoContentType: "image/png",
            logoSizeInBytes: 2048);
        var partial = SeriesDetailResponseFactory.Create(logoContainer: "bowlneba-public");

        // Act
        var fullModel = full.ToViewModel();
        var partialModel = partial.ToViewModel();

        // Assert
        var logo = fullModel.Logo.ShouldNotBeNull();
        logo.Container.ShouldBe("bowlneba-public");
        logo.Path.ShouldBe("series/logo/a.png");
        logo.ContentType.ShouldBe("image/png");
        logo.SizeInBytes.ShouldBe(2048);
        partialModel.Logo.ShouldBeNull();
    }
}