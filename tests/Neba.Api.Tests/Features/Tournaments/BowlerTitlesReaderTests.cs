using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class BowlerTitlesReaderTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private Season _season = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _season = SeasonFactory.Create(startDate: new DateOnly(2010, 1, 1), endDate: new DateOnly(2030, 12, 31));
        await _dbContext.Seasons.AddAsync(_season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private BowlerTitlesReader CreateReader() => new(_dbContext);

    [Fact(DisplayName = "LoadAsync returns nothing when no bowlers are given")]
    public async Task LoadAsync_ShouldReturnEmpty_WhenNoBowlersGiven()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var titles = await CreateReader().LoadAsync([], ct);

        // Assert
        titles.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "LoadAsync returns historical and recorded titles for each requested bowler")]
    public async Task LoadAsync_ShouldReturnBothSources_ForEachBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        var other = BowlerFactory.Create();
        var notRequested = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler, other, notRequested], ct);

        var historical = TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(2015, 5, 1),
            endDate: new DateOnly(2015, 5, 2),
            seasonId: _season.Id);
        await _dbContext.Tournaments.AddAsync(historical, ct);
        await _dbContext.HistoricalTournamentChampions.AddRangeAsync(
            [
                HistoricalTournamentChampionFactory.Create(bowler, historical),
                HistoricalTournamentChampionFactory.Create(notRequested, historical)
            ],
            ct);

        var recorded = TournamentFactory.Create(
            tournamentType: TournamentType.Singles,
            startDate: new DateOnly(2025, 3, 1),
            endDate: new DateOnly(2025, 3, 2),
            seasonId: _season.Id,
            status: TournamentStatus.Completed);
        recorded.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 50);
        recorded.AddResult(other.Id, place: 2, prizeMoney: 300m, points: 40);
        await _dbContext.Tournaments.AddAsync(recorded, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var titles = await CreateReader().LoadAsync([bowler.Id, other.Id], ct);

        // Assert
        titles[bowler.Id].ShouldBe(
            [new NebaTitle(new DateOnly(2015, 5, 1), new DateOnly(2015, 5, 2)), new NebaTitle(new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 2))],
            ignoreOrder: true);
        titles[other.Id].ShouldBeEmpty();
        titles.Contains(notRequested.Id).ShouldBeFalse();
    }

    [Fact(DisplayName = "LoadAsync skips first places in tournaments that are not title eligible")]
    public async Task LoadAsync_ShouldSkipTournamentsThatAreNotTitleEligible()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var truncated = TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(2025, 3, 1),
            endDate: new DateOnly(2025, 3, 2),
            seasonId: _season.Id,
            status: TournamentStatus.Truncated);
        truncated.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 50);

        var tooSmall = TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(2025, 4, 1),
            endDate: new DateOnly(2025, 4, 2),
            seasonId: _season.Id,
            status: TournamentStatus.Completed,
            entryCountForTitleEligibility: 1);
        tooSmall.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 50);

        await _dbContext.Tournaments.AddRangeAsync([truncated, tooSmall], ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var titles = await CreateReader().LoadAsync([bowler.Id], ct);

        // Assert
        titles[bowler.Id].ShouldBeEmpty();
    }
}