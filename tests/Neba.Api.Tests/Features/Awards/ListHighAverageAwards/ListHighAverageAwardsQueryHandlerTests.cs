using Neba.Api.Database;
using Neba.Api.Features.Awards.ListHighAverageAwards;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;

namespace Neba.Api.Tests.Features.Awards.ListHighAverageAwards;

[IntegrationTest]
[Component("Awards")]
[Collection<PostgreSqlFixture>]
public sealed class ListHighAverageAwardsQueryHandlerTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "HandleAsync returns empty collection when no seasons exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoSeasonsExist()
    {
        var handler = new ListHighAverageAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(
            new ListHighAverageAwardsQuery(),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns high average award with correct fields when data exists")]
    public async Task HandleAsync_ShouldReturnAward_WithCorrectFields_WhenDataExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Jane", "Doe"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        const decimal average = 210m;
        const int totalGames = 30;
        const int tournamentsParticipated = 6;
        const int statEligibleTournamentCount = 6; // minimum = floor(4.5 × 6) = 27 games

        var season = SeasonFactory.Create(complete: true, description: "2025 Season");
        season.AddHighAverageWinner(bowler.Id, average, totalGames, tournamentsParticipated, statEligibleTournamentCount);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListHighAverageAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListHighAverageAwardsQuery(), ct);

        result.ShouldHaveSingleItem();
        var award = result.Single();
        award.Season.ShouldBe("2025 Season");
        award.BowlerName.ShouldBe(bowler.Name);
        award.Average.ShouldBe(average);
        award.TotalGames.ShouldBe(totalGames);
        award.TournamentsParticipated.ShouldBe(tournamentsParticipated);
    }

    [Fact(DisplayName = "HandleAsync returns awards from all seasons when multiple complete seasons exist")]
    public async Task HandleAsync_ShouldReturnAwardsFromAllSeasons_WhenMultipleSeasonsExist()
    {
        var ct = TestContext.Current.CancellationToken;
        const int seed = 12;
        var bowlers = BowlerFactory.Bogus(10, seed);
        await _dbContext.Bowlers.AddRangeAsync(bowlers, ct);

        var seasons = SeasonFactory.Bogus(4, [.. bowlers.Select(b => b.Id)], seed: seed);
        await _dbContext.Seasons.AddRangeAsync(seasons, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListHighAverageAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListHighAverageAwardsQuery(), ct);

        foreach (var award in result)
        {
            award.Season.ShouldNotBeNullOrWhiteSpace();
            award.Average.ShouldBeGreaterThan(0m);
        }
    }
}
