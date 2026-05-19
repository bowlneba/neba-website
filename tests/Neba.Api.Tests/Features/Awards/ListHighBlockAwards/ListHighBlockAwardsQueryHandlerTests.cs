using Neba.Api.Database;
using Neba.Api.Features.Awards.ListHighBlockAwards;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;

namespace Neba.Api.Tests.Features.Awards.ListHighBlockAwards;

[IntegrationTest]
[Component("Awards")]
[Collection<PostgreSqlFixture>]
public sealed class ListHighBlockAwardsQueryHandlerTests(PostgreSqlFixture fixture)
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
        var handler = new ListHighBlockAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(
            new ListHighBlockAwardsQuery(),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns high block award with correct fields when data exists")]
    public async Task HandleAsync_ShouldReturnAward_WithCorrectFields_WhenDataExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Jane", "Doe"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        const int blockScore = 1150;
        const int games = 5;

        var season = SeasonFactory.Create(complete: true, description: "2025 Season");
        season.AddHighBlockWinner(bowler.Id, blockScore, games);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListHighBlockAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListHighBlockAwardsQuery(), ct);

        result.ShouldHaveSingleItem();
        var award = result.Single();
        award.Season.ShouldBe("2025 Season");
        award.BowlerName.ShouldBe(bowler.Name);
        award.Score.ShouldBe(blockScore);
    }

    [Fact(DisplayName = "HandleAsync returns awards from all seasons when multiple complete seasons exist")]
    public async Task HandleAsync_ShouldReturnAwardsFromAllSeasons_WhenMultipleSeasonsExist()
    {
        var ct = TestContext.Current.CancellationToken;
        const int seed = 13;
        var bowlers = BowlerFactory.Bogus(10, seed);
        await _dbContext.Bowlers.AddRangeAsync(bowlers, ct);

        var seasons = SeasonFactory.Bogus(4, [.. bowlers.Select(b => b.Id)], seed: seed);
        await _dbContext.Seasons.AddRangeAsync(seasons, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListHighBlockAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListHighBlockAwardsQuery(), ct);

        foreach (var award in result)
        {
            award.Season.ShouldNotBeNullOrWhiteSpace();
            award.Score.ShouldBeGreaterThan(0);
        }
    }
}