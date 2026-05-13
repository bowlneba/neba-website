using ErrorOr;

using Neba.Application.Stats;
using Neba.Application.Stats.BoyProgression;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Stats;

namespace Neba.Application.Tests.Stats.GetSeasonStats;

[UnitTest]
[Component("Stats")]
public sealed class GetSeasonStatsQueryHandlerTests
{
    private readonly Mock<ISeasonStatsService> _seasonStatsServiceMock;
    private readonly Mock<IBowlerOfTheYearProgressionService> _boyProgressionServiceMock;
    private readonly GetSeasonStatsQueryHandler _handler;

    public GetSeasonStatsQueryHandlerTests()
    {
        _seasonStatsServiceMock = new Mock<ISeasonStatsService>(MockBehavior.Strict);
        _boyProgressionServiceMock = new Mock<IBowlerOfTheYearProgressionService>(MockBehavior.Strict);

        _handler = new GetSeasonStatsQueryHandler(
            _seasonStatsServiceMock.Object,
            _boyProgressionServiceMock.Object);
    }

    [Fact(DisplayName = "Should return SeasonHasNoStats when no seasons with stats exist")]
    public async Task HandleAsync_ShouldReturnSeasonHasNoStats_WhenNoSeasonsExist()
    {
        // Arrange
        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([]);

        var query = new GetSeasonStatsQuery { SeasonYear = null };

        // Act
        ErrorOr<SeasonStatsDto> result = default;
        await Should.NotThrowAsync(async () => { result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken); });

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(StatsErrors.SeasonHasNoStats);
    }

    [Fact(DisplayName = "Should return SeasonHasNoStats when requested year does not match any season")]
    public async Task HandleAsync_ShouldReturnSeasonHasNoStats_WhenSeasonYearNotFound()
    {
        // Arrange
        var season = SeasonDtoFactory.Create(
            startDate: new DateOnly(2024, 9, 1),
            endDate: new DateOnly(2025, 8, 31));

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([season]);

        var query = new GetSeasonStatsQuery { SeasonYear = 2023 };

        // Act
        ErrorOr<SeasonStatsDto> result = default;
        await Should.NotThrowAsync(async () => { result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken); });

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(StatsErrors.SeasonHasNoStats);
    }

    [Fact(DisplayName = "Should select the season with the most recent EndDate when SeasonYear is null")]
    public async Task HandleAsync_ShouldSelectMostRecentSeason_WhenSeasonYearIsNull()
    {
        // Arrange
        var olderSeason = SeasonDtoFactory.Create(
            startDate: new DateOnly(2023, 9, 1),
            endDate: new DateOnly(2024, 8, 31));
        var newerSeason = SeasonDtoFactory.Create(
            startDate: new DateOnly(2024, 9, 1),
            endDate: new DateOnly(2025, 8, 31));

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([olderSeason, newerSeason]);

        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(3);
        var botyRaces = EmptyBotyRaces();
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(newerSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(newerSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(newerSeason, TestContext.Current.CancellationToken))
            .ReturnsAsync((45m, 5m, 7.5m));
        _seasonStatsServiceMock
            .Setup(s => s.CalculateSeasonStatsSummary(bowlerStats, 45m, 5m, 7.5m))
            .Returns(summary);

        // Canary setups for olderSeason: if OrderBy mutation fires and picks olderSeason instead of newerSeason,
        // the calls succeed (no MockException) so the Season assertion below can catch the mutation.
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(olderSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(olderSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(olderSeason, TestContext.Current.CancellationToken))
            .ReturnsAsync((45m, 5m, 7.5m));

        var query = new GetSeasonStatsQuery { SeasonYear = null };

        // Act
        ErrorOr<SeasonStatsDto> result = default;
        await Should.NotThrowAsync(async () => { result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken); });

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Season.ShouldBe(newerSeason);
    }

    [Fact(DisplayName = "Should match season by EndDate year when SeasonYear is specified")]
    public async Task HandleAsync_ShouldMatchSeasonByEndDateYear_WhenSeasonYearSpecified()
    {
        // Arrange
        var season = SeasonDtoFactory.Create(
            startDate: new DateOnly(2024, 9, 1),
            endDate: new DateOnly(2025, 8, 31));

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([season]);

        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(3);
        var botyRaces = EmptyBotyRaces();
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(season, TestContext.Current.CancellationToken))
            .ReturnsAsync((45m, 5m, 7.5m));
        _seasonStatsServiceMock
            .Setup(s => s.CalculateSeasonStatsSummary(bowlerStats, 45m, 5m, 7.5m))
            .Returns(summary);

        var query = new GetSeasonStatsQuery { SeasonYear = 2025 };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Season.ShouldBe(season);
    }

    [Fact(DisplayName = "Should match season by StartDate year when SeasonYear is specified")]
    public async Task HandleAsync_ShouldMatchSeasonByStartDateYear_WhenSeasonYearSpecified()
    {
        // Arrange
        var season = SeasonDtoFactory.Create(
            startDate: new DateOnly(2024, 9, 1),
            endDate: new DateOnly(2025, 8, 31));

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([season]);

        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(3);
        var botyRaces = EmptyBotyRaces();
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(season, TestContext.Current.CancellationToken))
            .ReturnsAsync((45m, 5m, 7.5m));
        _seasonStatsServiceMock
            .Setup(s => s.CalculateSeasonStatsSummary(bowlerStats, 45m, 5m, 7.5m))
            .Returns(summary);

        var query = new GetSeasonStatsQuery { SeasonYear = 2024 };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Season.ShouldBe(season);
    }

    [Fact(DisplayName = "Should select season by year when SeasonYear targets the non-most-recent season")]
    public async Task HandleAsync_ShouldSelectSeasonByYear_NotMostRecent_WhenSeasonYearProvided()
    {
        // Arrange — two seasons; SeasonYear = 2023 (older), not the most-recent 2024 season
        var olderSeason = SeasonDtoFactory.Create(
            startDate: new DateOnly(2022, 9, 1),
            endDate: new DateOnly(2023, 8, 31));
        var newerSeason = SeasonDtoFactory.Create(
            startDate: new DateOnly(2023, 9, 1),
            endDate: new DateOnly(2024, 8, 31));

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([olderSeason, newerSeason]);

        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(2);
        var botyRaces = EmptyBotyRaces();
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(olderSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(olderSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(olderSeason, TestContext.Current.CancellationToken))
            .ReturnsAsync((40m, 4m, 6m));
        _seasonStatsServiceMock
            .Setup(s => s.CalculateSeasonStatsSummary(bowlerStats, 40m, 4m, 6m))
            .Returns(summary);

        // Canary setups for newerSeason: if Conditional(false) mutation fires and always takes the
        // "most recent" branch, newerSeason is picked. Setting up its calls lets the Season assertion
        // below catch the mutation rather than relying on MockException.
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(newerSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(newerSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(newerSeason, TestContext.Current.CancellationToken))
            .ReturnsAsync((40m, 4m, 6m));

        var query = new GetSeasonStatsQuery { SeasonYear = 2023 };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert — must select the older season by year, not default to the most-recent newerSeason
        result.IsError.ShouldBeFalse();
        result.Value.Season.ShouldBe(olderSeason);
    }

    [Fact(DisplayName = "Should select the only matching season when SeasonYear matches a single season in the list")]
    public async Task HandleAsync_ShouldSelectOnlyMatchingSeason_WhenSeasonYearMatchesSingleSeason()
    {
        // Arrange
        var olderSeason = SeasonDtoFactory.Create(
            startDate: new DateOnly(2022, 9, 1),
            endDate: new DateOnly(2023, 8, 31));
        var matchingSeason = SeasonDtoFactory.Create(
            startDate: new DateOnly(2024, 9, 1),
            endDate: new DateOnly(2025, 8, 31));

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([olderSeason, matchingSeason]);

        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(2);
        var botyRaces = EmptyBotyRaces();
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(matchingSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(matchingSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(matchingSeason, TestContext.Current.CancellationToken))
            .ReturnsAsync((40m, 4m, 6m));
        _seasonStatsServiceMock
            .Setup(s => s.CalculateSeasonStatsSummary(bowlerStats, 40m, 4m, 6m))
            .Returns(summary);

        // Canary setups for the non-matching season: if the selection logic regresses and chooses the
        // wrong season, the assertions below will fail on the season value rather than a mock exception.
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(olderSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(olderSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(olderSeason, TestContext.Current.CancellationToken))
            .ReturnsAsync((40m, 4m, 6m));

        var query = new GetSeasonStatsQuery { SeasonYear = 2025 };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Season.ShouldBe(matchingSeason);
        result.Value.SeasonsWithStats.First().ShouldBe(matchingSeason);
    }

    [Fact(DisplayName = "Should return SeasonsWithStats ordered by EndDate descending")]
    public async Task HandleAsync_ShouldReturnSeasonsWithStats_OrderedByEndDateDescending()
    {
        // Arrange — three seasons returned out of order; result must be newest-first
        var season2023 = SeasonDtoFactory.Create(
            startDate: new DateOnly(2022, 9, 1),
            endDate: new DateOnly(2023, 8, 31));
        var season2025 = SeasonDtoFactory.Create(
            startDate: new DateOnly(2024, 9, 1),
            endDate: new DateOnly(2025, 8, 31));
        var season2024 = SeasonDtoFactory.Create(
            startDate: new DateOnly(2023, 9, 1),
            endDate: new DateOnly(2024, 8, 31));

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync([season2023, season2025, season2024]);

        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(1);
        var botyRaces = EmptyBotyRaces();
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(season2025.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(season2025.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(season2025, TestContext.Current.CancellationToken))
            .ReturnsAsync((45m, 5m, 7.5m));
        _seasonStatsServiceMock
            .Setup(s => s.CalculateSeasonStatsSummary(bowlerStats, 45m, 5m, 7.5m))
            .Returns(summary);

        var query = new GetSeasonStatsQuery { SeasonYear = null };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert — SeasonsWithStats must be newest-first regardless of return order from service
        result.IsError.ShouldBeFalse();
        var seasons = result.Value.SeasonsWithStats.ToArray();
        seasons[0].ShouldBe(season2025);
        seasons[1].ShouldBe(season2024);
        seasons[2].ShouldBe(season2023);
    }

    [Fact(DisplayName = "Should return fully populated SeasonStatsDto when season is found")]
    public async Task HandleAsync_ShouldReturnFullyPopulatedDto_WhenSeasonFound()
    {
        // Arrange
        var season = SeasonDtoFactory.Create();
        var allSeasons = new[] { season };
        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(5);
        var botyRaces = EmptyBotyRaces();
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(allSeasons);
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _boyProgressionServiceMock
            .Setup(s => s.GetAllProgressionsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRaces);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(season, TestContext.Current.CancellationToken))
            .ReturnsAsync((45m, 5m, 7.5m));
        _seasonStatsServiceMock
            .Setup(s => s.CalculateSeasonStatsSummary(bowlerStats, 45m, 5m, 7.5m))
            .Returns(summary);

        var query = new GetSeasonStatsQuery { SeasonYear = null };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Season.ShouldBe(season);
        result.Value.SeasonsWithStats.ShouldBe(allSeasons);
        result.Value.BowlerStats.ShouldBe(bowlerStats);
        result.Value.BowlerOfTheYearRaces.ShouldBe(botyRaces);
        result.Value.Summary.ShouldBe(summary);
        result.Value.MinimumNumberOfGames.ShouldBe(45m);
        result.Value.MinimumNumberOfTournaments.ShouldBe(5m);
        result.Value.MinimumNumberOfEntries.ShouldBe(7.5m);
    }

    private static IReadOnlyDictionary<int, IReadOnlyCollection<Application.Stats.GetSeasonStats.BowlerOfTheYearPointsRaceSeriesDto>> EmptyBotyRaces()
        => new Dictionary<int, IReadOnlyCollection<Application.Stats.GetSeasonStats.BowlerOfTheYearPointsRaceSeriesDto>>
        {
            [BowlerOfTheYearCategory.Open.Value] = [],
            [BowlerOfTheYearCategory.Senior.Value] = [],
            [BowlerOfTheYearCategory.SuperSenior.Value] = [],
            [BowlerOfTheYearCategory.Woman.Value] = [],
            [BowlerOfTheYearCategory.Youth.Value] = [],
            [BowlerOfTheYearCategory.Rookie.Value] = [],
        };
}