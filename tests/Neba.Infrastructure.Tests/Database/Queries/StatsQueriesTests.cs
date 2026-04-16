using Neba.Domain.Seasons;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Stats;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("Stats")]
[Collection<PostgreSqlFixture>]
public sealed class StatsQueriesTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly StatsQueries _queries;

    public StatsQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = fixture.CreateDbContext();
        _queries = new StatsQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetBowlerSeasonStatsAsync returns only stats for requested season")]
    public async Task GetBowlerSeasonStatsAsync_ShouldReturnOnlyStatsForRequestedSeason_WhenMultipleSeasonsExist()
    {
        // Arrange
        const int seed = 320;
        var requestedSeason = SeasonFactory.Bogus(1, seed: seed).Single();
        var otherSeason = SeasonFactory.Create(id: SeasonId.New(), description: "Other season");

        var bowlerInRequestedSeason = BowlerFactory.Create(
            name: NameFactory.Create(firstName: "Alex", lastName: "Lane"));
        var secondBowlerInRequestedSeason = BowlerFactory.Create(
            name: NameFactory.Create(firstName: "Casey", lastName: "Stone"));
        var bowlerInOtherSeason = BowlerFactory.Create(
            name: NameFactory.Create(firstName: "Jordan", lastName: "West"));

        var statsInRequestedSeason = new[]
        {
            BowlerSeasonStatsFactory.Create(
                seasonId: requestedSeason.Id,
                bowlerId: bowlerInRequestedSeason.Id),
            BowlerSeasonStatsFactory.Create(
                seasonId: requestedSeason.Id,
                bowlerId: secondBowlerInRequestedSeason.Id)
        };

        var statsInOtherSeason = BowlerSeasonStatsFactory.Create(
            seasonId: otherSeason.Id,
            bowlerId: bowlerInOtherSeason.Id);

        await _dbContext.Bowlers.AddRangeAsync(
            [bowlerInRequestedSeason, secondBowlerInRequestedSeason, bowlerInOtherSeason],
            TestContext.Current.CancellationToken);

        await _dbContext.Seasons.AddRangeAsync(
            [requestedSeason, otherSeason],
            TestContext.Current.CancellationToken);

        await _dbContext.BowlerSeasonStats.AddRangeAsync(
            [.. statsInRequestedSeason, statsInOtherSeason],
            TestContext.Current.CancellationToken);

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetBowlerSeasonStatsAsync(requestedSeason.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(stat => stat.BowlerId != bowlerInOtherSeason.Id);
        result.Select(stat => stat.BowlerId).ShouldBe([bowlerInRequestedSeason.Id, secondBowlerInRequestedSeason.Id], ignoreOrder: true);
    }

    [Fact(DisplayName = "GetBowlerSeasonStatsAsync maps all projected fields")]
    public async Task GetBowlerSeasonStatsAsync_ShouldMapProjectedFields_WhenStatsExist()
    {
        // Arrange
        const int seed = 321;
        var season = SeasonFactory.Bogus(1, seed: seed).Single();
        var bowler = BowlerFactory.Bogus(1, seed: seed).Single();

        var expectedLastUpdatedUtc = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);

        var stats = BowlerSeasonStatsFactory.Create(
            seasonId: season.Id,
            bowlerId: bowler.Id,
            isMember: false,
            isRookie: true,
            isSenior: true,
            isSuperSenior: false,
            isWoman: true,
            isYouth: false,
            eligibleTournaments: 7,
            totalTournaments: 9,
            eligibleEntries: 10,
            totalEntries: 13,
            cashes: 4,
            finals: 2,
            qualifyingHighGame: 298,
            highBlock: 1197,
            matchPlayWins: 5,
            matchPlayLosses: 3,
            matchPlayGames: 8,
            matchPlayPinfall: 1685,
            matchPlayHighGame: 267,
            totalGames: 99,
            totalPinfall: 20123,
            fieldAverage: 8.75m,
            highFinish: 1,
            averageFinish: 6.2m,
            bowlerOfTheYearPoints: 601,
            seniorOfTheYearPoints: 444,
            superSeniorOfTheYearPoints: 0,
            womanOfTheYearPoints: 333,
            youthOfTheYearPoints: 0,
            tournamentWinnings: 3210.50m,
            cupEarnings: 910.25m,
            credits: 45.75m,
            lastUpdatedUtc: expectedLastUpdatedUtc);

        await _dbContext.Bowlers.AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.BowlerSeasonStats.AddAsync(stats, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetBowlerSeasonStatsAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);

        await Verify(result);
    }

    [Fact(DisplayName = "GetSeasonsWithStatsAsync returns empty dictionary when no stats exist")]
    public async Task GetSeasonsWithStatsAsync_ShouldReturnEmptyDictionary_WhenNoStatsExist()
    {
        // Arrange — database is reset in InitializeAsync, no data seeded

        // Act
        var result = await _queries.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetSeasonsWithStatsAsync returns only seasons that have stats, each once")]
    public async Task GetSeasonsWithStatsAsync_ShouldReturnDistinctSeasonsWithStats_WhenMixedDataExists()
    {
        // Arrange
        const int seed = 400;
        var seasonWithStats = SeasonFactory.Bogus(1, seed: seed).Single();
        var anotherSeasonWithStats = SeasonFactory.Bogus(1, seed: seed + 1).Single();
        var seasonWithoutStats = SeasonFactory.Bogus(1, seed: seed + 2).Single();

        var bowler1 = BowlerFactory.Create();
        var bowler2 = BowlerFactory.Create();
        var bowler3 = BowlerFactory.Create();

        // Two stats entries for the same season — should appear only once in the result
        var stats1 = BowlerSeasonStatsFactory.Create(seasonId: seasonWithStats.Id, bowlerId: bowler1.Id);
        var stats2 = BowlerSeasonStatsFactory.Create(seasonId: seasonWithStats.Id, bowlerId: bowler2.Id);
        var stats3 = BowlerSeasonStatsFactory.Create(seasonId: anotherSeasonWithStats.Id, bowlerId: bowler3.Id);

        await _dbContext.Bowlers.AddRangeAsync([bowler1, bowler2, bowler3], TestContext.Current.CancellationToken);
        await _dbContext.Seasons.AddRangeAsync(
            [seasonWithStats, anotherSeasonWithStats, seasonWithoutStats],
            TestContext.Current.CancellationToken);
        await _dbContext.BowlerSeasonStats.AddRangeAsync([stats1, stats2, stats3], TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetSeasonsWithStatsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);

        await Verify(result);
    }
}