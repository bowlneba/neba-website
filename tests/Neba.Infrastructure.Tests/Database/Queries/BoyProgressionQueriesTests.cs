using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("Stats")]
[Collection<PostgreSqlFixture>]
public sealed class BoyProgressionQueriesTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly StatsQueries _queries;

    public BoyProgressionQueriesTests(PostgreSqlFixture fixture)
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

    [Fact(DisplayName = "GetBoyProgressionResultsForSeasonAsync returns only results for the requested season")]
    public async Task GetBoyProgressionResultsForSeasonAsync_ShouldReturnOnlyResultsForRequestedSeason()
    {
        // Arrange
        var requestedSeason = SeasonFactory.Create(id: SeasonId.New());
        var otherSeason = SeasonFactory.Create(id: SeasonId.New(), description: "Other season");

        var bowlerA = BowlerFactory.Create(name: NameFactory.Create(firstName: "Alex", lastName: "Lane"));
        var bowlerB = BowlerFactory.Create(name: NameFactory.Create(firstName: "Pat", lastName: "Cross"));

        var tournamentInRequestedSeason = TournamentFactory.Create(seasonId: requestedSeason.Id, statsEligible: true);
        var tournamentInOtherSeason = TournamentFactory.Create(seasonId: otherSeason.Id, statsEligible: true);

        await _dbContext.Seasons.AddRangeAsync([requestedSeason, otherSeason], TestContext.Current.CancellationToken);
        await _dbContext.Bowlers.AddRangeAsync([bowlerA, bowlerB], TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync([tournamentInRequestedSeason, tournamentInOtherSeason], TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.ChangeTracker.Clear();

        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowlerA, tournament: tournamentInRequestedSeason, points: 80));
        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowlerB, tournament: tournamentInOtherSeason, points: 60));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetBoyProgressionResultsForSeasonAsync(requestedSeason.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result.Single().BowlerId.ShouldBe(bowlerA.Id);
    }

    [Fact(DisplayName = "GetBoyProgressionResultsForSeasonAsync maps all projected fields correctly")]
    public async Task GetBoyProgressionResultsForSeasonAsync_ShouldMapAllProjectedFields()
    {
        // Arrange
        var season = SeasonFactory.Create(id: SeasonId.New());

        var dateOfBirth = new DateOnly(1975, 6, 15);
        var bowler = BowlerFactory.Create(
            name: NameFactory.Create(firstName: "Jordan", lastName: "West"),
            gender: Gender.Female,
            dateOfBirth: dateOfBirth);

        var startDate = new DateOnly(2025, 3, 10);
        var endDate = new DateOnly(2025, 3, 11);
        var tournament = TournamentFactory.Create(
            seasonId: season.Id,
            tournamentType: TournamentType.Senior,
            startDate: startDate,
            endDate: endDate,
            statsEligible: false);

        var sideCut = SideCutFactory.Create(name: "Senior");

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Bowlers.AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(tournament, TestContext.Current.CancellationToken);
        await _dbContext.SideCuts.AddAsync(sideCut, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.ChangeTracker.Clear();

        _dbContext.Add(HistoricalTournamentResultFactory.Create(
            bowler: bowler,
            tournament: tournament,
            points: 45,
            sideCut: sideCut));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetBoyProgressionResultsForSeasonAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        var row = result.Single();

        row.BowlerId.ShouldBe(bowler.Id);
        row.BowlerName.ShouldBe(bowler.Name);
        row.BowlerDateOfBirth.ShouldBe(dateOfBirth);
        row.BowlerGender.ShouldBe(Gender.Female);
        row.TournamentId.ShouldBe(tournament.Id);
        row.TournamentName.ShouldBe(tournament.Name);
        row.TournamentDate.ShouldBe(startDate);
        row.TournamentEndDate.ShouldBe(endDate);
        row.StatsEligible.ShouldBeFalse();
        row.TournamentType.ShouldBe(TournamentType.Senior);
        row.Points.ShouldBe(45);
        row.SideCutId.ShouldNotBeNull();
        row.SideCutName.ShouldBe("Senior");
    }

    [Fact(DisplayName = "GetBoyProgressionResultsForSeasonAsync maps null SideCutId and SideCutName for main-cut result")]
    public async Task GetBoyProgressionResultsForSeasonAsync_MainCutResult_ShouldHaveNullSideCutFields()
    {
        // Arrange
        var season = SeasonFactory.Create(id: SeasonId.New());
        var bowler = BowlerFactory.Create();
        var tournament = TournamentFactory.Create(seasonId: season.Id);

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Bowlers.AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(tournament, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.ChangeTracker.Clear();

        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowler, tournament: tournament, points: 120, sideCut: null));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetBoyProgressionResultsForSeasonAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        var row = result.Single();
        row.SideCutId.ShouldBeNull();
        row.SideCutName.ShouldBeNull();
        row.Points.ShouldBe(120);
    }

    [Fact(DisplayName = "GetBoyProgressionResultsForSeasonAsync returns results ordered by TournamentDate ascending")]
    public async Task GetBoyProgressionResultsForSeasonAsync_ShouldReturnResultsOrderedByTournamentDate()
    {
        // Arrange
        var season = SeasonFactory.Create(id: SeasonId.New());
        var bowler = BowlerFactory.Create();

        var tournamentJan = TournamentFactory.Create(seasonId: season.Id, name: "January", startDate: new DateOnly(2025, 1, 10), endDate: new DateOnly(2025, 1, 11));
        var tournamentMar = TournamentFactory.Create(seasonId: season.Id, name: "March",   startDate: new DateOnly(2025, 3, 10), endDate: new DateOnly(2025, 3, 11));
        var tournamentFeb = TournamentFactory.Create(seasonId: season.Id, name: "February", startDate: new DateOnly(2025, 2, 10), endDate: new DateOnly(2025, 2, 11));

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Bowlers.AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync([tournamentJan, tournamentMar, tournamentFeb], TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.ChangeTracker.Clear();

        // Seed in non-chronological order to verify ordering
        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowler, tournament: tournamentMar, points: 30));
        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowler, tournament: tournamentJan, points: 10));
        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowler, tournament: tournamentFeb, points: 20));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetBoyProgressionResultsForSeasonAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        var dates = result.Select(r => r.TournamentDate).ToArray();
        dates[0].ShouldBe(new DateOnly(2025, 1, 10));
        dates[1].ShouldBe(new DateOnly(2025, 2, 10));
        dates[2].ShouldBe(new DateOnly(2025, 3, 10));
    }

    [Fact(DisplayName = "GetBoyProgressionResultsForSeasonAsync includes both stat-eligible and non-stat-eligible tournaments")]
    public async Task GetBoyProgressionResultsForSeasonAsync_ShouldIncludeAllTournamentTypes_NotFilterByStatsEligible()
    {
        // Arrange — the query returns ALL rows; service is responsible for filtering by eligibility
        var season = SeasonFactory.Create(id: SeasonId.New());
        var bowler = BowlerFactory.Create();

        var statEligible = TournamentFactory.Create(seasonId: season.Id, statsEligible: true);
        var notStatEligible = TournamentFactory.Create(
            seasonId: season.Id,
            tournamentType: TournamentType.Senior,
            statsEligible: false);

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Bowlers.AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync([statEligible, notStatEligible], TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.ChangeTracker.Clear();

        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowler, tournament: statEligible, points: 80));
        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowler, tournament: notStatEligible, points: 40));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetBoyProgressionResultsForSeasonAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(r => r.StatsEligible);
        result.ShouldContain(r => !r.StatsEligible);
    }

    [Fact(DisplayName = "GetBoyProgressionResultsForSeasonAsync maps null bowler demographic fields when absent")]
    public async Task GetBoyProgressionResultsForSeasonAsync_ShouldMapNullDemographics_WhenBowlerHasNone()
    {
        // Arrange
        var season = SeasonFactory.Create(id: SeasonId.New());
        var bowler = new Bowler
        {
            Id = BowlerId.New(),
            Name = NameFactory.Create(),
            Gender = null,
            DateOfBirth = null
        };
        var tournament = TournamentFactory.Create(seasonId: season.Id);

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Bowlers.AddAsync(bowler, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(tournament, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.ChangeTracker.Clear();

        _dbContext.Add(HistoricalTournamentResultFactory.Create(bowler: bowler, tournament: tournament, points: 50));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetBoyProgressionResultsForSeasonAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        var row = result.Single();
        row.BowlerDateOfBirth.ShouldBeNull();
        row.BowlerGender.ShouldBeNull();
    }
}
