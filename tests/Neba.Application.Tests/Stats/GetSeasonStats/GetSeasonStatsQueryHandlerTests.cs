using Neba.Application.Bowlers;
using Neba.Application.Seasons;
using Neba.Application.Stats;
using Neba.Application.Stats.GetSeasonStats;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Stats;

namespace Neba.Application.Tests.Stats.GetSeasonStats;

[UnitTest]
[Component("Stats")]
public sealed class GetSeasonStatsQueryHandlerTests
{
    private readonly Mock<ISeasonStatsService> _seasonStatsServiceMock;
    private readonly Mock<IBowlerQueries> _bowlerQueriesMock;
    private readonly GetSeasonStatsQueryHandler _handler;

    public GetSeasonStatsQueryHandlerTests()
    {
        _seasonStatsServiceMock = new Mock<ISeasonStatsService>(MockBehavior.Strict);
        _bowlerQueriesMock = new Mock<IBowlerQueries>(MockBehavior.Strict);

        _handler = new GetSeasonStatsQueryHandler(
            _seasonStatsServiceMock.Object,
            _bowlerQueriesMock.Object);
    }

    [Fact(DisplayName = "Should return SeasonHasNoStats when no seasons with stats exist")]
    public async Task HandleAsync_ShouldReturnSeasonHasNoStats_WhenNoSeasonsExist()
    {
        // Arrange
        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(Array.Empty<SeasonDto>());

        var query = new GetSeasonStatsQuery { SeasonYear = null };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

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
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

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
        var botyRace = BowlerOfTheYearPointsRaceSeriesDtoFactory.Bogus(2);
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(newerSeason.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerOfTheYearRaceAsync(newerSeason, _bowlerQueriesMock.Object, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRace);
        _seasonStatsServiceMock
            .Setup(s => s.GetStatMinimumsForSeasonAsync(newerSeason, TestContext.Current.CancellationToken))
            .ReturnsAsync((45m, 5m, 7.5m));
        _seasonStatsServiceMock
            .Setup(s => s.CalculateSeasonStatsSummary(bowlerStats, 45m, 5m, 7.5m))
            .Returns(summary);

        var query = new GetSeasonStatsQuery { SeasonYear = null };

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

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
        var botyRace = BowlerOfTheYearPointsRaceSeriesDtoFactory.Bogus(2);
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerOfTheYearRaceAsync(season, _bowlerQueriesMock.Object, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRace);
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
        var botyRace = BowlerOfTheYearPointsRaceSeriesDtoFactory.Bogus(2);
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerOfTheYearRaceAsync(season, _bowlerQueriesMock.Object, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRace);
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

    [Fact(DisplayName = "Should return fully populated SeasonStatsDto when season is found")]
    public async Task HandleAsync_ShouldReturnFullyPopulatedDto_WhenSeasonFound()
    {
        // Arrange
        var season = SeasonDtoFactory.Create();
        var allSeasons = new[] { season };
        var bowlerStats = BowlerSeasonStatsDtoFactory.Bogus(5);
        var botyRace = BowlerOfTheYearPointsRaceSeriesDtoFactory.Bogus(3);
        var summary = SeasonStatsSummaryDtoFactory.Create();

        _seasonStatsServiceMock
            .Setup(s => s.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(allSeasons);
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerSeasonStatsAsync(season.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlerStats);
        _seasonStatsServiceMock
            .Setup(s => s.GetBowlerOfTheYearRaceAsync(season, _bowlerQueriesMock.Object, TestContext.Current.CancellationToken))
            .ReturnsAsync(botyRace);
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
        result.Value.BowlerOfTheYearRace.ShouldBe(botyRace);
        result.Value.Summary.ShouldBe(summary);
        result.Value.MinimumNumberOfGames.ShouldBe(45m);
        result.Value.MinimumNumberOfTournaments.ShouldBe(5m);
        result.Value.MinimumNumberOfEntries.ShouldBe(7.5m);
    }
}