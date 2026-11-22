using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class TournamentSuccessorCacheEvictionTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private ServiceProvider _serviceProvider = null!;
    private Season _season = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        var services = new ServiceCollection();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();

        _season = SeasonFactory.Create(startDate: new DateOnly(2020, 1, 1), endDate: new DateOnly(2030, 12, 31));
        await _dbContext.Seasons.AddAsync(_season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private async Task<Season> SeedOtherSeasonAsync(CancellationToken ct)
    {
        var season = SeasonFactory.Create(startDate: new DateOnly(2031, 1, 1), endDate: new DateOnly(2031, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        return season;
    }

    private async Task<Tournament> SeedAsync(
        int year, Tournament? previous, TournamentStatus? status, CancellationToken ct, Season? season = null)
    {
        var tournament = TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(year, 3, 7),
            endDate: new DateOnly(year, 3, 8),
            seasonId: (season ?? _season).Id,
            status: status,
            previousTournament: previous is null
                ? null
                : PreviousTournamentFactsFactory.Create(previous.Id, TournamentType.Doubles, previous.EndDate));
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    [Fact(DisplayName = "FindAsync returns nothing when no tournament follows this one")]
    public async Task FindAsync_ShouldReturnNothing_WhenNoSuccessor()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedAsync(2025, null, null, ct);

        // Act
        var successors = await TournamentSuccessorCacheEviction.FindAsync(_dbContext, tournament.Id, ct);

        // Assert
        successors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "FindAsync returns the tournament that follows this one")]
    public async Task FindAsync_ShouldReturnSuccessor_WhenOneFollows()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = await SeedAsync(2025, null, null, ct);
        var second = await SeedAsync(2026, first, TournamentStatus.Completed, ct);

        // Act
        var successors = await TournamentSuccessorCacheEviction.FindAsync(_dbContext, first.Id, ct);

        // Assert
        var successor = successors.ShouldHaveSingleItem();
        successor.Id.ShouldBe(second.Id);
        successor.SeasonId.ShouldBe(_season.Id);
    }

    [Fact(DisplayName = "FindAsync keeps walking through every successor, truncated or not, because a run of titles counts back through them")]
    public async Task FindAsync_ShouldContinue_ThroughEverySuccessor()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = await SeedAsync(2025, null, null, ct);
        var second = await SeedAsync(2026, first, TournamentStatus.Truncated, ct);
        var third = await SeedAsync(2027, second, TournamentStatus.Truncated, ct);
        var fourth = await SeedAsync(2028, third, null, ct);
        var fifth = await SeedAsync(2029, fourth, null, ct);

        // Act
        var successors = await TournamentSuccessorCacheEviction.FindAsync(_dbContext, first.Id, ct);

        // Assert
        successors.Select(successor => successor.Id).ShouldBe([second.Id, third.Id, fourth.Id, fifth.Id], ignoreOrder: true);
    }

    [Fact(DisplayName = "FindAsync reaches the tournament whose title run counts the first tournament, eleven steps on")]
    public async Task FindAsync_ShouldReachEleventhSuccessor_WhenChainIsLong()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var chain = new List<Tournament> { await SeedAsync(2020, null, null, ct) };

        for (var year = 2021; year <= 2031; year++)
        {
            chain.Add(await SeedAsync(year, chain[^1], null, ct));
        }

        // Act
        var successors = await TournamentSuccessorCacheEviction.FindAsync(_dbContext, chain[0].Id, ct);

        // Assert
        successors.Select(successor => successor.Id).ShouldBe(chain.Skip(1).Select(tournament => tournament.Id), ignoreOrder: true);
    }

    [Fact(DisplayName = "EvictAsync removes the cached detail and season list of each successor and leaves other entries")]
    public async Task EvictAsync_ShouldRemoveSuccessorTags_AndKeepOthers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = await SeedAsync(2025, null, null, ct);
        var second = await SeedAsync(2026, first, null, ct);
        var unrelated = await SeedAsync(2027, null, null, ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        await cache.GetOrSetAsync("successor-detail", _ => Task.FromResult("stale"), tags: [$"neba:tournaments:{second.Id}"], token: ct);
        await cache.GetOrSetAsync("successor-list", _ => Task.FromResult("stale"), tags: [$"neba:tournaments:{second.SeasonId}"], token: ct);
        await cache.GetOrSetAsync("unrelated", _ => Task.FromResult("kept"), tags: [$"neba:tournaments:{unrelated.Id}"], token: ct);

        // Act
        await TournamentSuccessorCacheEviction.EvictAsync(_dbContext, cache, first.Id, ct);

        // Assert
        (await cache.GetOrSetAsync("successor-detail", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("fresh");
        (await cache.GetOrSetAsync("successor-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("fresh");
        (await cache.GetOrSetAsync("unrelated", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("kept");
    }

    [Fact(DisplayName = "EvictPredecessorSeasonsAsync removes the cached season list of each given tournament's season and leaves other seasons")]
    public async Task EvictPredecessorSeasonsAsync_ShouldRemoveSeasonTag_AndKeepOtherSeasons()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedOtherSeasonAsync(ct);
        var predecessor = await SeedAsync(2025, null, null, ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        await cache.GetOrSetAsync("predecessor-list", _ => Task.FromResult("stale"), tags: [$"neba:tournaments:{_season.Id}"], token: ct);
        await cache.GetOrSetAsync("other-list", _ => Task.FromResult("kept"), tags: [$"neba:tournaments:{otherSeason.Id}"], token: ct);

        // Act
        await TournamentSuccessorCacheEviction.EvictPredecessorSeasonsAsync(_dbContext, cache, [predecessor.Id], ct);

        // Assert
        (await cache.GetOrSetAsync("predecessor-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("fresh");
        (await cache.GetOrSetAsync("other-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("kept");
    }

    [Fact(DisplayName = "EvictPredecessorSeasonsAsync removes the cached season list of every season when the tournaments sit in different seasons")]
    public async Task EvictPredecessorSeasonsAsync_ShouldRemoveEverySeasonTag_WhenSeasonsDiffer()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedOtherSeasonAsync(ct);
        var first = await SeedAsync(2025, null, null, ct);
        var second = await SeedAsync(2031, null, null, ct, otherSeason);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        await cache.GetOrSetAsync("first-list", _ => Task.FromResult("stale"), tags: [$"neba:tournaments:{_season.Id}"], token: ct);
        await cache.GetOrSetAsync("second-list", _ => Task.FromResult("stale"), tags: [$"neba:tournaments:{otherSeason.Id}"], token: ct);

        // Act
        await TournamentSuccessorCacheEviction.EvictPredecessorSeasonsAsync(_dbContext, cache, [first.Id, second.Id, first.Id], ct);

        // Assert
        (await cache.GetOrSetAsync("first-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("fresh");
        (await cache.GetOrSetAsync("second-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("fresh");
    }

    [Fact(DisplayName = "EvictPredecessorSeasonsAsync skips null ids and evicts only the tournaments that are given")]
    public async Task EvictPredecessorSeasonsAsync_ShouldSkipNullIds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var otherSeason = await SeedOtherSeasonAsync(ct);
        var predecessor = await SeedAsync(2031, null, null, ct, otherSeason);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        await cache.GetOrSetAsync("main-list", _ => Task.FromResult("kept"), tags: [$"neba:tournaments:{_season.Id}"], token: ct);
        await cache.GetOrSetAsync("other-list", _ => Task.FromResult("stale"), tags: [$"neba:tournaments:{otherSeason.Id}"], token: ct);

        // Act
        await TournamentSuccessorCacheEviction.EvictPredecessorSeasonsAsync(_dbContext, cache, [null, predecessor.Id], ct);

        // Assert
        (await cache.GetOrSetAsync("main-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("kept");
        (await cache.GetOrSetAsync("other-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("fresh");
    }

    [Theory(DisplayName = "EvictPredecessorSeasonsAsync evicts nothing when no tournament id is given")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EvictPredecessorSeasonsAsync_ShouldEvictNothing_WhenNoIdsGiven(bool withNullId)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        await cache.GetOrSetAsync("main-list", _ => Task.FromResult("kept"), tags: [$"neba:tournaments:{_season.Id}"], token: ct);
        IEnumerable<TournamentId?> ids = withNullId ? [null] : [];

        // Act
        await TournamentSuccessorCacheEviction.EvictPredecessorSeasonsAsync(_dbContext, cache, ids, ct);

        // Assert
        (await cache.GetOrSetAsync("main-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("kept");
    }

    [Fact(DisplayName = "EvictPredecessorSeasonsAsync evicts nothing when the tournament no longer exists")]
    public async Task EvictPredecessorSeasonsAsync_ShouldEvictNothing_WhenTournamentMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        await cache.GetOrSetAsync("main-list", _ => Task.FromResult("kept"), tags: [$"neba:tournaments:{_season.Id}"], token: ct);

        // Act
        await TournamentSuccessorCacheEviction.EvictPredecessorSeasonsAsync(_dbContext, cache, [TournamentId.New()], ct);

        // Assert
        (await cache.GetOrSetAsync("main-list", _ => Task.FromResult("fresh"), token: ct)).ShouldBe("kept");
    }
}
