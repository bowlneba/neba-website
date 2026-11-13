using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.HallOfFame.ListHallOfFamePoints;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.HallOfFame.ListHallOfFamePoints;

[IntegrationTest]
[Component("HallOfFame")]
[Collection<AppDbContextFixture>]
public sealed class ListHallOfFamePointsQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private int _legacyMembershipId = 1;
    private int _seasonCounter = 1;

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private ListHallOfFamePointsQueryHandler CreateHandler()
        => new(new HallOfFamePointsReader(_dbContext), new HallOfFamePointsCalculator());

    private async Task<Bowler> AddBowlerAsync(string first, string last)
    {
        var bowler = BowlerFactory.Create(name: NameFactory.Create(first, last));
        await _dbContext.Bowlers.AddAsync(bowler, TestContext.Current.CancellationToken);
        return bowler;
    }

    /// <summary>Adds a historical champion title in its own season, so each call is a different season.</summary>
    private async Task AddTitleAsync(Bowler bowler, TournamentType type, int year)
    {
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(
            description: $"Season {year}-{_seasonCounter++}",
            startDate: new DateOnly(year, 1, 1),
            endDate: new DateOnly(year, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(
            tournamentType: type,
            startDate: new DateOnly(year, 5, 1),
            endDate: new DateOnly(year, 5, 2),
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.HistoricalTournamentChampions.AddAsync(
            HistoricalTournamentChampionFactory.Create(bowler, tournament), ct);
    }

    private void AddMembership(Bowler bowler, MembershipType type, int year)
        => bowler.AddLegacyMembership(
            _legacyMembershipId++, type, 0m, new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)).IsError.ShouldBeFalse();

    private void AddRenewals(Bowler bowler, int count, int firstYear = 2010, bool withNew = true)
    {
        for (var i = 0; i < count; i++)
        {
            var type = withNew && i == 0 ? MembershipType.New : MembershipType.Renewal;
            AddMembership(bowler, type, firstYear + i);
        }
    }

    [Fact(DisplayName = "HandleAsync returns the point table, thresholds and no standings when nobody has points")]
    public async Task HandleAsync_ShouldReturnRulesAndNoStandings_WhenNobodyHasPoints()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new ListHallOfFamePointsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Standings.ShouldBeEmpty();
        result.Rules.ShouldNotBeEmpty();
        result.RequiredPoints.ShouldBe(36);
        result.RequiredSeasonsWithPoints.ShouldBe(3);
        result.RequiredMembershipYears.ShouldBe(10);
        result.MembershipDataStartYear.ShouldBe(1999);
    }

    [Fact(DisplayName = "HandleAsync excludes a bowler inducted for Superior Performance")]
    public async Task HandleAsync_ShouldExcludeSuperiorPerformanceInductee()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var inductee = await AddBowlerAsync("Ray", "Dunleavy");
        var other = await AddBowlerAsync("Tom", "Davis");
        await AddTitleAsync(inductee, TournamentType.Singles, 2010);
        await AddTitleAsync(other, TournamentType.Singles, 2011);
        await _dbContext.HallOfFameInductions.AddAsync(
            HallOfFameInductionFactory.Create(bowlerId: inductee.Id, categories: [HallOfFameCategory.SuperiorPerformance]), ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new ListHallOfFamePointsQuery(), ct);

        // Assert
        result.Standings.ShouldHaveSingleItem().BowlerName.ShouldBe("Tom Davis");
    }

    [Fact(DisplayName = "HandleAsync keeps a bowler inducted only for Meritorious Service and sets InductedCategories")]
    public async Task HandleAsync_ShouldKeepMeritoriousServiceInductee()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await AddBowlerAsync("Walt", "Brennan");
        await AddTitleAsync(bowler, TournamentType.Masters, 2010);
        await _dbContext.HallOfFameInductions.AddAsync(
            HallOfFameInductionFactory.Create(bowlerId: bowler.Id, categories: [HallOfFameCategory.MeritoriousService]), ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new ListHallOfFamePointsQuery(), ct);

        // Assert
        var standing = result.Standings.ShouldHaveSingleItem();
        standing.BowlerName.ShouldBe("Walt Brennan");
        standing.TotalPoints.ShouldBe(6);
        standing.InductedCategories.ShouldBe(["Meritorious Service"]);
    }

    [Fact(DisplayName = "HandleAsync excludes bowlers whose wins earn no points")]
    public async Task HandleAsync_ShouldExcludeZeroPointBowlers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await AddBowlerAsync("Zed", "Zero");
        await AddTitleAsync(bowler, TournamentType.Youth, 2010);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new ListHallOfFamePointsQuery(), ct);

        // Assert
        result.Standings.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync is Eligible only when points, seasons and membership years all pass")]
    public async Task HandleAsync_ShouldBeEligible_OnlyWhenAllThreeCriteriaPass()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var eligible = await AddBowlerAsync("Marcus", "Hale");
        for (var i = 0; i < 6; i++) await AddTitleAsync(eligible, TournamentType.Singles, 2005 + i);
        AddRenewals(eligible, 10);

        var twoSeasons = await AddBowlerAsync("Two", "Seasons");
        var sharedSeason = SeasonFactory.Create(description: "shared season", startDate: new DateOnly(2008, 1, 1), endDate: new DateOnly(2008, 12, 31));
        var otherSeason = SeasonFactory.Create(description: "other season", startDate: new DateOnly(2009, 1, 1), endDate: new DateOnly(2009, 12, 31));
        await _dbContext.Seasons.AddRangeAsync([sharedSeason, otherSeason], ct);
        var seasonIds = new[] { sharedSeason.Id, sharedSeason.Id, sharedSeason.Id, sharedSeason.Id, otherSeason.Id, otherSeason.Id };
        foreach (var seasonId in seasonIds)
        {
            var tournament = TournamentFactory.Create(tournamentType: TournamentType.Singles, seasonId: seasonId,
                startDate: new DateOnly(2008, 5, 1), endDate: new DateOnly(2008, 5, 2));
            await _dbContext.Tournaments.AddAsync(tournament, ct);
            await _dbContext.HistoricalTournamentChampions.AddAsync(HistoricalTournamentChampionFactory.Create(twoSeasons, tournament), ct);
        }

        AddRenewals(twoSeasons, 12);

        var fewYears = await AddBowlerAsync("Sue", "Whitfield");
        for (var i = 0; i < 6; i++) await AddTitleAsync(fewYears, TournamentType.Singles, 2005 + i);
        AddRenewals(fewYears, 8);

        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new ListHallOfFamePointsQuery(), ct);

        // Assert
        var byName = result.Standings.ToDictionary(standing => standing.BowlerName);

        byName["Marcus Hale"].Eligible.ShouldBeTrue();

        var seasonsShort = byName["Two Seasons"];
        seasonsShort.TotalPoints.ShouldBe(36);
        seasonsShort.SeasonsWithPoints.ShouldBe(2);
        seasonsShort.MeetsPoints.ShouldBeTrue();
        seasonsShort.MeetsSeasons.ShouldBeFalse();
        seasonsShort.Eligible.ShouldBeFalse();

        var yearsShort = byName["Sue Whitfield"];
        yearsShort.TotalPoints.ShouldBe(36);
        yearsShort.MembershipYears.ShouldBe(8);
        yearsShort.MeetsPoints.ShouldBeTrue();
        yearsShort.Eligible.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync orders by points high to low, then by name")]
    public async Task HandleAsync_ShouldOrderByPointsThenName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var low = await AddBowlerAsync("Aaron", "Low");
        var bTie = await AddBowlerAsync("Beth", "Tie");
        var aTie = await AddBowlerAsync("Alan", "Tie");
        var high = await AddBowlerAsync("Zoe", "High");
        await AddTitleAsync(low, TournamentType.Trios, 2010);
        await AddTitleAsync(bTie, TournamentType.Singles, 2011);
        await AddTitleAsync(aTie, TournamentType.Singles, 2012);
        await AddTitleAsync(high, TournamentType.Singles, 2013);
        await AddTitleAsync(high, TournamentType.Singles, 2014);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new ListHallOfFamePointsQuery(), ct);

        // Assert
        result.Standings.Select(standing => standing.BowlerName)
            .ShouldBe(["Zoe High", "Alan Tie", "Beth Tie", "Aaron Low"]);
    }

    [Fact(DisplayName = "HandleAsync counts titles from historical champions and recorded results, plus awards")]
    public async Task HandleAsync_ShouldCountAllPointSources()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = await AddBowlerAsync("Jane", "Doe");
        await AddTitleAsync(bowler, TournamentType.Singles, 2010);

        var season = SeasonFactory.Create(description: "recorded", startDate: new DateOnly(2024, 1, 1), endDate: new DateOnly(2024, 12, 31), complete: true);
        var tournament = TournamentFactory.Create(tournamentType: TournamentType.Doubles, seasonId: season.Id,
            startDate: new DateOnly(2024, 4, 1), endDate: new DateOnly(2024, 4, 2));
        tournament.CompleteTournament(entryCount: 100);
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 50);
        season.AddBowlerOfTheYearWinner(bowler.Id);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new ListHallOfFamePointsQuery(), ct);

        // Assert
        var standing = result.Standings.ShouldHaveSingleItem();
        standing.TotalPoints.ShouldBe(6 + 6 + 12);
        standing.SeasonsWithPoints.ShouldBe(2);
    }

    [Fact(DisplayName = "HandleAsync reports membership years and an incomplete history for bowlers with points")]
    public async Task HandleAsync_ShouldReportMembershipYearsAndHistory()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var complete = await AddBowlerAsync("Cora", "Complete");
        var incomplete = await AddBowlerAsync("Ian", "Incomplete");
        var guest = await AddBowlerAsync("Gus", "Guest");
        var noMembership = await AddBowlerAsync("Nora", "None");
        var noPoints = await AddBowlerAsync("Pat", "NoPoints");
        foreach (var (bowler, year) in new[] { (complete, 2010), (incomplete, 2011), (guest, 2012), (noMembership, 2013) })
        {
            await AddTitleAsync(bowler, TournamentType.Singles, year);
        }

        AddRenewals(complete, 3);
        AddRenewals(incomplete, 4, withNew: false);
        AddMembership(guest, MembershipType.Guest, 2012);
        AddMembership(noPoints, MembershipType.Renewal, 2015);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new ListHallOfFamePointsQuery(), ct);

        // Assert
        var byName = result.Standings.ToDictionary(standing => standing.BowlerName);
        byName.ShouldNotContainKey("Pat NoPoints");
        byName["Cora Complete"].MembershipYears.ShouldBe(3);
        byName["Cora Complete"].MembershipHistoryComplete.ShouldBeTrue();
        byName["Ian Incomplete"].MembershipYears.ShouldBe(4);
        byName["Ian Incomplete"].MembershipHistoryComplete.ShouldBeFalse();
        byName["Gus Guest"].MembershipYears.ShouldBe(0);
        byName["Gus Guest"].MembershipHistoryComplete.ShouldBeTrue();
        byName["Nora None"].MembershipYears.ShouldBe(0);
        byName["Nora None"].MembershipHistoryComplete.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync records a list cache miss")]
    public async Task HandleAsync_ShouldRecordCacheMiss_WhenHandled()
    {
        // Arrange
        using var recorder = new CacheMissRecorder();

        // Act
        await CreateHandler().HandleAsync(new ListHallOfFamePointsQuery(), TestContext.Current.CancellationToken);

        // Assert
        recorder.Queries.ShouldContain("list");
    }
}
