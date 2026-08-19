using Neba.Api.Database;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListChampions;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListChampions;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class ListChampionsQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "HandleAsync returns empty collection when no historical champions exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoHistoricalChampionsExist()
    {
        // Arrange
        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns tournament with correct fields when a single champion record exists")]
    public async Task HandleAsync_ShouldReturnTournamentWithCorrectFields_WhenSingleChampionRecordExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowler = BowlerFactory.Create(name: NameFactory.Create("Alice", "Smith"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var tournament = TournamentFactory.Create(
            name: "NEBA Singles 2024",
            tournamentType: TournamentType.Singles,
            startDate: new DateOnly(2024, 10, 4),
            endDate: new DateOnly(2024, 10, 5),
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.HistoricalTournamentChampions.AddAsync(new HistoricalTournamentChampion
        {
            Bowler = bowler,
            Tournament = tournament
        }, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.TournamentId.ShouldBe(tournament.Id);
        dto.TournamentName.ShouldBe("NEBA Singles 2024");
        dto.TournamentDate.ShouldBe(new DateOnly(2024, 10, 5));
        dto.TournamentType.ShouldBe(TournamentType.Singles.Name);
        dto.Champions.ShouldHaveSingleItem();
        var champion = dto.Champions.Single();
        champion.BowlerId.ShouldBe(bowler.Id);
        champion.BowlerName.ShouldBe(bowler.Name);
        champion.HallOfFame.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync groups multiple champions under the same tournament")]
    public async Task HandleAsync_ShouldGroupMultipleChampions_UnderSameTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowlerA = BowlerFactory.Create();
        var bowlerB = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowlerA, bowlerB], ct);

        var tournament = TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.HistoricalTournamentChampions.AddRangeAsync(
            new HistoricalTournamentChampion { Bowler = bowlerA, Tournament = tournament },
            new HistoricalTournamentChampion { Bowler = bowlerB, Tournament = tournament });
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.TournamentId.ShouldBe(tournament.Id);
        dto.Champions.Count.ShouldBe(2);
        dto.Champions.Select(c => c.BowlerId).ShouldBe([bowlerA.Id, bowlerB.Id], ignoreOrder: true);
    }

    [Fact(DisplayName = "HandleAsync returns separate entries for each tournament")]
    public async Task HandleAsync_ShouldReturnSeparateEntry_ForEachTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowlerA = BowlerFactory.Create();
        var bowlerB = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowlerA, bowlerB], ct);

        var tournamentA = TournamentFactory.Create(name: "Singles 2024", seasonId: season.Id);
        var tournamentB = TournamentFactory.Create(name: "Doubles 2024", seasonId: season.Id);
        await _dbContext.Tournaments.AddRangeAsync([tournamentA, tournamentB], ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.HistoricalTournamentChampions.AddRangeAsync(
            new HistoricalTournamentChampion { Bowler = bowlerA, Tournament = tournamentA },
            new HistoricalTournamentChampion { Bowler = bowlerB, Tournament = tournamentB });
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.Count.ShouldBe(2);
        result.Select(r => r.TournamentId).ShouldBe([tournamentA.Id, tournamentB.Id], ignoreOrder: true);
    }

    [Fact(DisplayName = "HandleAsync sets HallOfFame true when bowler has an induction")]
    public async Task HandleAsync_ShouldSetHallOfFameTrue_WhenBowlerHasInduction()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var induction = HallOfFameInductionFactory.Create(bowlerId: bowler.Id);
        await _dbContext.HallOfFameInductions.AddAsync(induction, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.HistoricalTournamentChampions.AddAsync(new HistoricalTournamentChampion
        {
            Bowler = bowler,
            Tournament = tournament
        }, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().Champions.Single().HallOfFame.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync sets HallOfFame false when bowler has no inductions")]
    public async Task HandleAsync_ShouldSetHallOfFameFalse_WhenBowlerHasNoInductions()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.HistoricalTournamentChampions.AddAsync(new HistoricalTournamentChampion
        {
            Bowler = bowler,
            Tournament = tournament
        }, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().Champions.Single().HallOfFame.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync does not mix champions across tournaments")]
    public async Task HandleAsync_ShouldNotMixChampions_AcrossTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowlerA = BowlerFactory.Create();
        var bowlerB = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowlerA, bowlerB], ct);

        var tournamentA = TournamentFactory.Create(seasonId: season.Id);
        var tournamentB = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddRangeAsync([tournamentA, tournamentB], ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.HistoricalTournamentChampions.AddRangeAsync(
            new HistoricalTournamentChampion { Bowler = bowlerA, Tournament = tournamentA },
            new HistoricalTournamentChampion { Bowler = bowlerB, Tournament = tournamentB });
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.Count.ShouldBe(2);
        var dtoA = result.Single(r => r.TournamentId == tournamentA.Id);
        var dtoB = result.Single(r => r.TournamentId == tournamentB.Id);
        dtoA.Champions.ShouldHaveSingleItem();
        dtoA.Champions.Single().BowlerId.ShouldBe(bowlerA.Id);
        dtoB.Champions.ShouldHaveSingleItem();
        dtoB.Champions.Single().BowlerId.ShouldBe(bowlerB.Id);
    }

    [Fact(DisplayName = "HandleAsync includes a tournament whose champion comes from a recorded 1st place result, not historical data")]
    public async Task HandleAsync_ShouldIncludeTournament_WhenChampionComesFromRecordedResult()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowler = BowlerFactory.Create(name: NameFactory.Create("Alice", "Smith"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var tournament = TournamentFactory.Create(
            name: "NEBA Singles 2026",
            tournamentType: TournamentType.Singles,
            startDate: new DateOnly(2026, 3, 7),
            endDate: new DateOnly(2026, 3, 8),
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        tournament.CompleteTournament();
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 50);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.TournamentId.ShouldBe(tournament.Id);
        dto.TournamentName.ShouldBe("NEBA Singles 2026");
        dto.TournamentDate.ShouldBe(new DateOnly(2026, 3, 8));
        dto.TournamentType.ShouldBe(TournamentType.Singles.Name);
        dto.Champions.ShouldHaveSingleItem();
        var champion = dto.Champions.Single();
        champion.BowlerId.ShouldBe(bowler.Id);
        champion.BowlerName.ShouldBe(bowler.Name);
    }

    [Fact(DisplayName = "HandleAsync excludes recorded results that are not 1st place")]
    public async Task HandleAsync_ShouldExcludeRecordedResult_WhenPlaceIsNotFirst()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        tournament.CompleteTournament();
        tournament.AddResult(bowler.Id, place: 2, prizeMoney: 250m, points: 30);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync excludes the canceled 2026 finals tournament even when a bowler placed 1st")]
    public async Task HandleAsync_ShouldExcludeRecordedResult_WhenTournamentIsTheCancelledFinals()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2026, 2, 21),
            endDate: new DateOnly(2026, 2, 22),
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        tournament.CompleteTournament();
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 1000m, points: 100);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns entries for both a historical champion and a recorded champion")]
    public async Task HandleAsync_ShouldReturnEntries_ForBothHistoricalAndRecordedChampions()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var historicalBowler = BowlerFactory.Create();
        var recordedBowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([historicalBowler, recordedBowler], ct);

        var historicalTournament = TournamentFactory.Create(name: "Historical 2020", seasonId: season.Id);
        var recordedTournament = TournamentFactory.Create(name: "Recorded 2026", seasonId: season.Id);
        await _dbContext.Tournaments.AddRangeAsync([historicalTournament, recordedTournament], ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.HistoricalTournamentChampions.AddAsync(new HistoricalTournamentChampion
        {
            Bowler = historicalBowler,
            Tournament = historicalTournament
        }, ct);

        recordedTournament.CompleteTournament();
        recordedTournament.AddResult(recordedBowler.Id, place: 1, prizeMoney: 500m, points: 50);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListChampionsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListChampionsQuery(), ct);

        // Assert
        result.Count.ShouldBe(2);
        result.Select(r => r.TournamentId).ShouldBe([historicalTournament.Id, recordedTournament.Id], ignoreOrder: true);
    }
}