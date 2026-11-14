using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
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
public sealed class DefendingTeamsReaderTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private Season _season = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _season = SeasonFactory.Create(startDate: new DateOnly(2020, 1, 1), endDate: new DateOnly(2030, 12, 31));
        await _dbContext.Seasons.AddAsync(_season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private DefendingTeamsReader CreateReader() => new(_dbContext);

    private async Task<(Bowler First, Bowler Second)> SeedPairAsync(CancellationToken ct)
    {
        var first = BowlerFactory.Create();
        var second = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([first, second], ct);
        await _dbContext.SaveChangesAsync(ct);
        return (first, second);
    }

    private async Task<Tournament> SeedTournamentAsync(
        int year,
        Tournament? previous,
        TournamentStatus status,
        (Bowler First, Bowler Second)? winners,
        CancellationToken ct,
        string? name = null)
    {
        var tournament = TournamentFactory.Create(
            name: name ?? ("Doubles " + year),
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(year, 3, 7),
            endDate: new DateOnly(year, 3, 8),
            seasonId: _season.Id,
            status: status,
            previousTournament: previous is null
                ? null
                : PreviousTournamentFactsFactory.Create(previous.Id, TournamentType.Doubles, previous.EndDate));

        if (winners is { } pair)
        {
            tournament.AddResult(pair.First.Id, place: 1, prizeMoney: 100m, points: 10);
            tournament.AddResult(pair.Second.Id, place: 1, prizeMoney: 100m, points: 10);
        }

        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        return tournament;
    }

    [Fact(DisplayName = "LoadAsync returns nothing when no tournaments are given")]
    public async Task LoadAsync_ShouldReturnEmpty_WhenNoTournamentsGiven()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await CreateReader().LoadAsync([], ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "LoadAsync returns no team when the tournament has no previous tournament")]
    public async Task LoadAsync_ShouldReturnNoTeam_WhenThereIsNoPreviousTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(2026, null, TournamentStatus.Scheduled, null, ct);

        // Act
        var result = await CreateReader().LoadAsync([tournament.Id], ct);

        // Assert
        result.ShouldNotContainKey(tournament.Id);
    }

    [Fact(DisplayName = "LoadAsync returns the previous tournament's winners as one team when it was completed")]
    public async Task LoadAsync_ShouldReturnOneTeam_WhenPreviousWasCompleted()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var winners = await SeedPairAsync(ct);
        var previous = await SeedTournamentAsync(2025, null, TournamentStatus.Completed, winners, ct, name: "Joe's Doubles");
        var current = await SeedTournamentAsync(2026, previous, TournamentStatus.Scheduled, null, ct);

        // Act
        var result = await CreateReader().LoadAsync([current.Id], ct);

        // Assert
        var team = result[current.Id].ShouldHaveSingleItem();
        team.SourceTournamentId.ShouldBe(previous.Id);
        team.SourceTournamentName.ShouldBe("Joe's Doubles");
        team.SeasonDescription.ShouldBe(_season.Description);
        team.SourceTruncated.ShouldBeFalse();
        team.Undefended.ShouldBeFalse();
        team.Champions.Select(champion => champion.BowlerId).ShouldBe([winners.First.Id, winners.Second.Id], ignoreOrder: true);
        team.Champions.Select(champion => champion.Name).ShouldBe([winners.First.Name, winners.Second.Name], ignoreOrder: true);
    }

    [Fact(DisplayName = "LoadAsync adds the winners before a truncated previous tournament, marked as undefended")]
    public async Task LoadAsync_ShouldReturnTwoTeams_WhenPreviousWasTruncated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var olderWinners = await SeedPairAsync(ct);
        var truncatedWinners = await SeedPairAsync(ct);
        var older = await SeedTournamentAsync(2025, null, TournamentStatus.Completed, olderWinners, ct);
        var truncated = await SeedTournamentAsync(2026, older, TournamentStatus.Truncated, truncatedWinners, ct);
        var current = await SeedTournamentAsync(2027, truncated, TournamentStatus.Scheduled, null, ct);

        // Act
        var result = await CreateReader().LoadAsync([current.Id], ct);

        // Assert
        var teams = result[current.Id];
        teams.Count.ShouldBe(2);
        teams[0].SourceTournamentId.ShouldBe(truncated.Id);
        teams[0].SourceTruncated.ShouldBeTrue();
        teams[0].Undefended.ShouldBeFalse();
        teams[1].SourceTournamentId.ShouldBe(older.Id);
        teams[1].SourceTruncated.ShouldBeFalse();
        teams[1].Undefended.ShouldBeTrue();
    }

    [Fact(DisplayName = "LoadAsync keeps walking back while each tournament reached is truncated")]
    public async Task LoadAsync_ShouldReturnThreeTeams_WhenTwoTruncatedInARow()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var winners2024 = await SeedPairAsync(ct);
        var winners2025 = await SeedPairAsync(ct);
        var winners2026 = await SeedPairAsync(ct);
        var first = await SeedTournamentAsync(2024, null, TournamentStatus.Completed, winners2024, ct);
        var second = await SeedTournamentAsync(2025, first, TournamentStatus.Truncated, winners2025, ct);
        var third = await SeedTournamentAsync(2026, second, TournamentStatus.Truncated, winners2026, ct);
        var current = await SeedTournamentAsync(2027, third, TournamentStatus.Scheduled, null, ct);

        // Act
        var result = await CreateReader().LoadAsync([current.Id], ct);

        // Assert
        result[current.Id].Select(team => team.SourceTournamentId).ShouldBe([third.Id, second.Id, first.Id]);
    }

    [Fact(DisplayName = "LoadAsync returns one team when the truncated previous tournament has no previous tournament")]
    public async Task LoadAsync_ShouldReturnOneTeam_WhenTruncatedHasNoPrevious()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var winners = await SeedPairAsync(ct);
        var truncated = await SeedTournamentAsync(2026, null, TournamentStatus.Truncated, winners, ct);
        var current = await SeedTournamentAsync(2027, truncated, TournamentStatus.Scheduled, null, ct);

        // Act
        var result = await CreateReader().LoadAsync([current.Id], ct);

        // Assert
        var team = result[current.Id].ShouldHaveSingleItem();
        team.SourceTruncated.ShouldBeTrue();
        team.Undefended.ShouldBeFalse();
    }

    [Fact(DisplayName = "LoadAsync returns no team when the previous tournament has no results")]
    public async Task LoadAsync_ShouldReturnNoTeam_WhenPreviousHasNoResults()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var previous = await SeedTournamentAsync(2025, null, TournamentStatus.Completed, null, ct);
        var current = await SeedTournamentAsync(2026, previous, TournamentStatus.Scheduled, null, ct);

        // Act
        var result = await CreateReader().LoadAsync([current.Id], ct);

        // Assert
        result[current.Id].ShouldBeEmpty();
    }

    [Fact(DisplayName = "LoadAsync counts historical champions as winners")]
    public async Task LoadAsync_ShouldCountHistoricalChampions()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var winners = await SeedPairAsync(ct);
        var previous = await SeedTournamentAsync(2018, null, TournamentStatus.Completed, null, ct);
        await _dbContext.HistoricalTournamentChampions.AddRangeAsync(
            [
                HistoricalTournamentChampionFactory.Create(winners.First, previous),
                HistoricalTournamentChampionFactory.Create(winners.Second, previous)
            ],
            ct);
        await _dbContext.SaveChangesAsync(ct);
        var current = await SeedTournamentAsync(2019, previous, TournamentStatus.Scheduled, null, ct);

        // Act
        var result = await CreateReader().LoadAsync([current.Id], ct);

        // Assert
        result[current.Id].ShouldHaveSingleItem().Champions
            .Select(champion => champion.BowlerId).ShouldBe([winners.First.Id, winners.Second.Id], ignoreOrder: true);
    }

    [Fact(DisplayName = "LoadAsync loads several tournaments in one call")]
    public async Task LoadAsync_ShouldLoadSeveralTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var winners2025 = await SeedPairAsync(ct);
        var winners2026 = await SeedPairAsync(ct);
        var previous2025 = await SeedTournamentAsync(2025, null, TournamentStatus.Completed, winners2025, ct);
        var previous2026 = await SeedTournamentAsync(2026, null, TournamentStatus.Completed, winners2026, ct);
        var current2026 = await SeedTournamentAsync(2026, previous2025, TournamentStatus.Scheduled, null, ct, name: "Other 2026");
        var current2027 = await SeedTournamentAsync(2027, previous2026, TournamentStatus.Scheduled, null, ct);
        var noPrevious = await SeedTournamentAsync(2028, null, TournamentStatus.Scheduled, null, ct);

        // Act
        var result = await CreateReader().LoadAsync([current2026.Id, current2027.Id, noPrevious.Id], ct);

        // Assert
        result[current2026.Id].ShouldHaveSingleItem().SourceTournamentId.ShouldBe(previous2025.Id);
        result[current2027.Id].ShouldHaveSingleItem().SourceTournamentId.ShouldBe(previous2026.Id);
        result.ShouldNotContainKey(noPrevious.Id);
    }
}