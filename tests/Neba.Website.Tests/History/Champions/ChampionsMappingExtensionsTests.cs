using Neba.Api.Contracts.Bowlers.GetBowlerTitles;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.History.Champions;

namespace Neba.Website.Tests.History.Champions;

[UnitTest]
[Component("Website.History.Champions.BowlerTitleMappingExtensions")]
public sealed class ChampionsMappingExtensionsTests
{
    // ── ToTitleSummaries ──────────────────────────────────────────────────

    [Fact(DisplayName = "Groups champions by BowlerId and counts titles correctly")]
    public void ToTitleSummaries_ShouldGroupByBowlerAndCountTitles()
    {
        // Arrange
        var champion = ChampionResponseFactory.Create(bowlerId: BowlerId.New(), bowlerName: "Jane Smith");
        var t1 = TournamentChampionResponseFactory.Create(champions: [champion]);
        var t2 = TournamentChampionResponseFactory.Create(champions: [champion]);

        // Act
        var result = new[] { t1, t2 }.ToTitleSummaries();

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().TitleCount.ShouldBe(2);
    }

    [Fact(DisplayName = "Maps BowlerId and BowlerName from champion response")]
    public void ToTitleSummaries_ShouldMapBowlerIdAndName()
    {
        // Arrange
        var bowlerId = BowlerId.New();
        var champion = ChampionResponseFactory.Create(bowlerId: bowlerId, bowlerName: "Joe Bowler");
        var tournament = TournamentChampionResponseFactory.Create(champions: [champion]);

        // Act
        var result = new[] { tournament }.ToTitleSummaries();

        // Assert
        var summary = result.Single();
        summary.BowlerId.ShouldBe(bowlerId.Value.ToString());
        summary.BowlerName.ShouldBe("Joe Bowler");
    }

    [Fact(DisplayName = "Maps HallOfFame from first champion entry in group")]
    public void ToTitleSummaries_ShouldMapHallOfFameFromFirstEntry()
    {
        // Arrange
        var champion = ChampionResponseFactory.Create(bowlerId: BowlerId.New(), hallOfFame: true);
        var t1 = TournamentChampionResponseFactory.Create(champions: [champion]);
        var t2 = TournamentChampionResponseFactory.Create(champions: [champion]);

        // Act
        var result = new[] { t1, t2 }.ToTitleSummaries();

        // Assert
        result.Single().HallOfFame.ShouldBeTrue();
    }

    [Fact(DisplayName = "Orders summaries by TitleCount descending")]
    public void ToTitleSummaries_ShouldOrderByTitleCountDescending()
    {
        // Arrange
        var bowler1 = ChampionResponseFactory.Create(bowlerId: BowlerId.New(), bowlerName: "One Title");
        var bowler2 = ChampionResponseFactory.Create(bowlerId: BowlerId.New(), bowlerName: "Three Titles");
        var single = TournamentChampionResponseFactory.Create(champions: [bowler1]);
        var multi1 = TournamentChampionResponseFactory.Create(champions: [bowler2]);
        var multi2 = TournamentChampionResponseFactory.Create(champions: [bowler2]);
        var multi3 = TournamentChampionResponseFactory.Create(champions: [bowler2]);

        // Act
        var result = new[] { single, multi1, multi2, multi3 }.ToTitleSummaries().ToList();

        // Assert
        result[0].BowlerName.ShouldBe("Three Titles");
        result[1].BowlerName.ShouldBe("One Title");
    }

    [Fact(DisplayName = "Returns empty collection when input is empty")]
    public void ToTitleSummaries_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        // Act
        var result = Array.Empty<Neba.Api.Contracts.Tournaments.ListChampions.TournamentChampionResponse>().ToTitleSummaries();

        // Assert
        result.ShouldBeEmpty();
    }

    // ── ToTitlesByYear ────────────────────────────────────────────────────

    [Fact(DisplayName = "Groups tournaments by year")]
    public void ToTitlesByYear_ShouldGroupByYear()
    {
        // Arrange
        var t2024a = TournamentChampionResponseFactory.Create(tournamentDate: new DateOnly(2024, 3, 1));
        var t2024b = TournamentChampionResponseFactory.Create(tournamentDate: new DateOnly(2024, 9, 1));
        var t2023 = TournamentChampionResponseFactory.Create(tournamentDate: new DateOnly(2023, 5, 1));

        // Act
        var result = new[] { t2024a, t2024b, t2023 }.ToTitlesByYear().ToList();

        // Assert
        result.Count.ShouldBe(2);
        result[0].Year.ShouldBe(2024);
        result[1].Year.ShouldBe(2023);
    }

    [Fact(DisplayName = "Orders year groups descending so newest year appears first")]
    public void ToTitlesByYear_ShouldOrderYearsDescending()
    {
        // Arrange
        var t2021 = TournamentChampionResponseFactory.Create(tournamentDate: new DateOnly(2021, 1, 1));
        var t2024 = TournamentChampionResponseFactory.Create(tournamentDate: new DateOnly(2024, 1, 1));
        var t2019 = TournamentChampionResponseFactory.Create(tournamentDate: new DateOnly(2019, 1, 1));

        // Act
        var result = new[] { t2021, t2024, t2019 }.ToTitlesByYear().ToList();

        // Assert
        result[0].Year.ShouldBe(2024);
        result[1].Year.ShouldBe(2021);
        result[2].Year.ShouldBe(2019);
    }

    [Fact(DisplayName = "Maps TournamentMonth and TournamentYear from TournamentDate")]
    public void ToTitlesByYear_ShouldMapMonthAndYearFromDate()
    {
        // Arrange
        var tournament = TournamentChampionResponseFactory.Create(
            tournamentDate: new DateOnly(2023, 7, 15));

        // Act
        var result = new[] { tournament }.ToTitlesByYear();

        // Assert
        var titleVm = result.Single().Titles.Single();
        titleVm.TournamentMonth.ShouldBe(7);
        titleVm.TournamentYear.ShouldBe(2023);
    }

    [Fact(DisplayName = "Returns empty collection when input is empty")]
    public void ToTitlesByYear_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        // Act
        var result = Array.Empty<Neba.Api.Contracts.Tournaments.ListChampions.TournamentChampionResponse>().ToTitlesByYear();

        // Assert
        result.ShouldBeEmpty();
    }

    // ── BowlerTitlesResponse.ToViewModel ─────────────────────────────────

    [Fact(DisplayName = "Maps BowlerName and HallOfFame from response")]
    public void ToViewModel_ShouldMapBowlerNameAndHallOfFame()
    {
        // Arrange
        var response = new BowlerTitlesResponse
        {
            BowlerName = "Sam Champion",
            HallOfFame = true,
            Titles = [],
        };

        // Act
        var result = response.ToViewModel();

        // Assert
        result.BowlerName.ShouldBe("Sam Champion");
        result.HallOfFame.ShouldBeTrue();
    }

    [Fact(DisplayName = "Formats TournamentDate as MMM yyyy display string")]
    public void ToViewModel_ShouldFormatTournamentDateAsMmmYyyy()
    {
        // Arrange
        var response = new BowlerTitlesResponse
        {
            BowlerName = "Joe",
            HallOfFame = false,
            Titles =
            [
                new BowlerTitleResponse
                {
                    TournamentId   = "t1",
                    TournamentName = "Singles Classic",
                    TournamentDate = new DateOnly(2024, 4, 1),
                    TournamentType = TournamentType.Singles.Name,
                },
            ],
        };

        // Act
        var result = response.ToViewModel();

        // Assert
        result.Titles.Single().TournamentDate.ShouldBe("Apr 2024");
    }

    [Fact(DisplayName = "Sorts titles most-recent-first by TournamentDate")]
    public void ToViewModel_ShouldSortTitlesMostRecentFirst()
    {
        // Arrange
        var response = new BowlerTitlesResponse
        {
            BowlerName = "Joe",
            HallOfFame = false,
            Titles =
            [
                new BowlerTitleResponse
                {
                    TournamentId   = "old",
                    TournamentName = "Old Classic",
                    TournamentDate = new DateOnly(2018, 3, 1),
                    TournamentType = TournamentType.Singles.Name,
                },
                new BowlerTitleResponse
                {
                    TournamentId   = "new",
                    TournamentName = "New Classic",
                    TournamentDate = new DateOnly(2024, 9, 1),
                    TournamentType = TournamentType.Singles.Name,
                },
            ],
        };

        // Act
        var result = response.ToViewModel();

        // Assert
        var titles = result.Titles.ToList();
        titles[0].TournamentId.ShouldBe("new");
        titles[1].TournamentId.ShouldBe("old");
    }

    [Fact(DisplayName = "Maps TournamentId, TournamentName, and TournamentType from title response")]
    public void ToViewModel_ShouldMapTitleFields()
    {
        // Arrange
        var response = new BowlerTitlesResponse
        {
            BowlerName = "Joe",
            HallOfFame = false,
            Titles =
            [
                new BowlerTitleResponse
                {
                    TournamentId   = "t-abc",
                    TournamentName = "Doubles Open",
                    TournamentDate = new DateOnly(2022, 6, 1),
                    TournamentType = TournamentType.Doubles.Name,
                },
            ],
        };

        // Act
        var result = response.ToViewModel();

        // Assert
        var title = result.Titles.Single();
        title.TournamentId.ShouldBe("t-abc");
        title.TournamentName.ShouldBe("Doubles Open");
        title.TournamentType.ShouldBe(TournamentType.Doubles.Name);
    }

    [Fact(DisplayName = "Returns empty Titles collection when response has no titles")]
    public void ToViewModel_ShouldReturnEmptyTitles_WhenResponseHasNone()
    {
        // Arrange
        var response = new BowlerTitlesResponse
        {
            BowlerName = "Joe",
            HallOfFame = false,
            Titles = [],
        };

        // Act
        var result = response.ToViewModel();

        // Assert
        result.Titles.ShouldBeEmpty();
    }
}