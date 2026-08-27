using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Stats;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Stats.GetSeasonStats;

[IntegrationTest]
[Component("Stats")]
[Collection<AppDbContextFixture>]
public sealed class GetSeasonStatsQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        var services = new ServiceCollection();
        services.AddHybridCache();
        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private GetSeasonStatsQueryHandler CreateHandler()
    {
        var cache = _serviceProvider.GetRequiredService<HybridCache>();
        return new GetSeasonStatsQueryHandler(
            _dbContext,
            new SeasonStatsCalculator(),
            new BowlerOfTheYearRaceCalculator(),
            cache);
    }

    [Fact(DisplayName = "HandleAsync returns SeasonHasNoStats when no BowlerSeasonStats exist")]
    public async Task HandleAsync_ShouldReturnSeasonHasNoStats_WhenNoBowlerSeasonStatsExist()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = null },
            TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Stats.SeasonHasNoStats");
    }

    [Fact(DisplayName = "HandleAsync returns stats for most recent season when no year specified")]
    public async Task HandleAsync_ShouldReturnStatsForMostRecentSeason_WhenNoYearSpecified()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var stats = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddAsync(stats, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = null }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Season.Id.ShouldBe(season.Id);
        result.Value.BowlerStats.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync returns stats for specified year when SeasonYear matches")]
    public async Task HandleAsync_ShouldReturnStatsForSpecifiedYear_WhenSeasonYearMatches()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season2024 = SeasonFactory.Create(
            startDate: new DateOnly(2024, 1, 1),
            endDate: new DateOnly(2024, 12, 31));
        var season2025 = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddRangeAsync([season2024, season2025], ct);

        var stats2024 = BowlerSeasonStatsFactory.Create(seasonId: season2024.Id, bowlerId: bowler.Id);
        var stats2025 = BowlerSeasonStatsFactory.Create(seasonId: season2025.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddRangeAsync([stats2024, stats2025], ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = 2024 }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Season.Id.ShouldBe(season2024.Id);
    }

    [Fact(DisplayName = "HandleAsync returns a single SeasonsWithStats entry when multiple bowlers exist for the same season")]
    public async Task HandleAsync_ShouldReturnSingleSeasonsWithStatsEntry_WhenMultipleBowlersExistForSameSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler1 = BowlerFactory.Create();
        var bowler2 = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler1, bowler2], ct);

        var season = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var stats1 = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler1.Id);
        var stats2 = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler2.Id);
        await _dbContext.BowlerSeasonStats.AddRangeAsync([stats1, stats2], ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = null }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.SeasonsWithStats.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "HandleAsync returns SeasonHasNoStats when specified year has no stats")]
    public async Task HandleAsync_ShouldReturnSeasonHasNoStats_WhenSpecifiedYearHasNoStats()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var stats = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddAsync(stats, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            new GetSeasonStatsQuery { SeasonYear = 2020 }, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Stats.SeasonHasNoStats");
    }

    [Fact(DisplayName = "HandleAsync includes BOY progression from TournamentResult when no HistoricalTournamentResult rows exist")]
    public async Task HandleAsync_ShouldIncludeBoyProgression_FromTournamentResultOnly()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id, statsEligible: true);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        tournament.CompleteTournament();
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 100);
        await _dbContext.SaveChangesAsync(ct);

        var stats = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddAsync(stats, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new GetSeasonStatsQuery { SeasonYear = 2026 }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var openSeries = result.Value.BowlerOfTheYearRaces[BowlerOfTheYearCategory.Open.Value];
        openSeries.ShouldHaveSingleItem();
        var series = openSeries.Single();
        series.BowlerId.ShouldBe(bowler.Id);
        series.Results.Single().CumulativePoints.ShouldBe(100);
    }

    [Fact(DisplayName = "HandleAsync unions HistoricalTournamentResult and TournamentResult rows for the same bowler in chronological order")]
    public async Task HandleAsync_ShouldUnionHistoricalAndCurrentResults_ForSameBowlerInChronologicalOrder()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var historicalTournament = TournamentFactory.Create(
            seasonId: season.Id, statsEligible: true,
            startDate: new DateOnly(2026, 1, 5), endDate: new DateOnly(2026, 1, 6));
        var currentTournament = TournamentFactory.Create(
            seasonId: season.Id, statsEligible: true,
            startDate: new DateOnly(2026, 2, 5), endDate: new DateOnly(2026, 2, 6));
        await _dbContext.Tournaments.AddRangeAsync([historicalTournament, currentTournament], ct);
        await _dbContext.SaveChangesAsync(ct);

        var historicalResult = HistoricalTournamentResultFactory.Create(
            bowler: bowler, tournament: historicalTournament, points: 100);
        await _dbContext.HistoricalTournamentResults.AddAsync(historicalResult, ct);
        await _dbContext.SaveChangesAsync(ct);

        currentTournament.CompleteTournament();
        currentTournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 150);
        await _dbContext.SaveChangesAsync(ct);

        var stats = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddAsync(stats, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new GetSeasonStatsQuery { SeasonYear = 2026 }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var openSeries = result.Value.BowlerOfTheYearRaces[BowlerOfTheYearCategory.Open.Value];
        openSeries.ShouldHaveSingleItem();
        var series = openSeries.Single();
        series.Results.Count.ShouldBe(2);
        series.Results.First().CumulativePoints.ShouldBe(100);
        series.Results.Last().CumulativePoints.ShouldBe(250);
    }

    [Fact(DisplayName = "HandleAsync still includes BOY progression from HistoricalTournamentResult alone")]
    public async Task HandleAsync_ShouldIncludeBoyProgression_FromHistoricalTournamentResultOnly()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create(
            startDate: new DateOnly(2024, 1, 1),
            endDate: new DateOnly(2024, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id, statsEligible: true);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var historicalResult = HistoricalTournamentResultFactory.Create(
            bowler: bowler, tournament: tournament, points: 100);
        await _dbContext.HistoricalTournamentResults.AddAsync(historicalResult, ct);

        var stats = BowlerSeasonStatsFactory.Create(seasonId: season.Id, bowlerId: bowler.Id);
        await _dbContext.BowlerSeasonStats.AddAsync(stats, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new GetSeasonStatsQuery { SeasonYear = 2024 }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var openSeries = result.Value.BowlerOfTheYearRaces[BowlerOfTheYearCategory.Open.Value];
        openSeries.ShouldHaveSingleItem();
        openSeries.Single().Results.Single().CumulativePoints.ShouldBe(100);
    }
}