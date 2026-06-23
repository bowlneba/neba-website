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
[Collection<PostgreSqlFixture>]
public sealed class ListChampionsQueryHandlerTests(PostgreSqlFixture fixture)
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
}