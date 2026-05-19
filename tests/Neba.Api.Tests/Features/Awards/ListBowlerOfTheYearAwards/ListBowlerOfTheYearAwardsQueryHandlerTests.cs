using Neba.Api.Database;
using Neba.Api.Features.Awards.ListBowlerOfTheYearAwards;
using Neba.Api.Features.Seasons.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;

namespace Neba.Api.Tests.Features.Awards.ListBowlerOfTheYearAwards;

[IntegrationTest]
[Component("Awards")]
[Collection<PostgreSqlFixture>]
public sealed class ListBowlerOfTheYearAwardsQueryHandlerTests(PostgreSqlFixture fixture)
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
        var handler = new ListBowlerOfTheYearAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(
            new ListBowlerOfTheYearAwardsQuery(),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns all BOY awards with correct fields when complete seasons exist")]
    public async Task HandleAsync_ShouldReturnAllAwards_WhenDataExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Jane", "Doe"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create(complete: true, description: "2025 Season");
        season.AddOpenBowlerOfTheYearWinner(bowler.Id);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListBowlerOfTheYearAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListBowlerOfTheYearAwardsQuery(), ct);

        result.ShouldHaveSingleItem();
        var award = result.Single();
        award.Season.ShouldBe("2025 Season");
        award.BowlerName.ShouldBe(bowler.Name);
        award.Category.ShouldBe(BowlerOfTheYearCategory.Open.Name);
    }

    [Fact(DisplayName = "HandleAsync returns awards from all complete seasons")]
    public async Task HandleAsync_ShouldReturnAwardsFromAllSeasons_WhenMultipleSeasonsExist()
    {
        var ct = TestContext.Current.CancellationToken;
        const int seed = 11;
        var bowlers = BowlerFactory.Bogus(10, seed);
        await _dbContext.Bowlers.AddRangeAsync(bowlers, ct);

        var seasons = SeasonFactory.Bogus(4, [.. bowlers.Select(b => b.Id)], seed: seed);
        await _dbContext.Seasons.AddRangeAsync(seasons, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListBowlerOfTheYearAwardsQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListBowlerOfTheYearAwardsQuery(), ct);

        foreach (var award in result)
        {
            award.Season.ShouldNotBeNullOrWhiteSpace();
            award.Category.ShouldNotBeNullOrWhiteSpace();
        }
    }
}