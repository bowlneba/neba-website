using Neba.Api.Database;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.GetBowlerTitles;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Bowlers.GetBowlerTitles;

[IntegrationTest]
[Component("Bowlers")]
[Collection<PostgreSqlFixture>]
public sealed class GetBowlerTitlesQueryHandlerTests(PostgreSqlFixture fixture)
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

    [Fact(DisplayName = "HandleAsync returns NotFound error when bowler does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBowlerDoesNotExist()
    {
        // Arrange
        var handler = new GetBowlerTitlesQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(
            new GetBowlerTitlesQuery { BowlerId = BowlerId.New() },
            TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Bowler.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns bowler name and empty titles when bowler has no champion records")]
    public async Task HandleAsync_ShouldReturnEmptyTitles_WhenBowlerHasNoChampionRecords()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Jane", "Doe"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new GetBowlerTitlesQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(
            new GetBowlerTitlesQuery { BowlerId = bowler.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerName.ShouldBe(bowler.Name);
        result.Value.HallOfFame.ShouldBeFalse();
        result.Value.Titles.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns HallOfFame true when bowler has an induction")]
    public async Task HandleAsync_ShouldReturnHallOfFameTrue_WhenBowlerHasInduction()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var induction = HallOfFameInductionFactory.Create(bowlerId: bowler.Id);
        await _dbContext.HallOfFameInductions.AddAsync(induction, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new GetBowlerTitlesQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(
            new GetBowlerTitlesQuery { BowlerId = bowler.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.HallOfFame.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync returns mapped title when bowler has a historical champion record")]
    public async Task HandleAsync_ShouldReturnMappedTitle_WhenBowlerHasHistoricalChampionRecord()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(
            name: "NEBA Singles",
            tournamentType: TournamentType.Singles,
            endDate: new DateOnly(2024, 10, 5),
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        _dbContext.HistoricalTournamentChampions.Add(new HistoricalTournamentChampion
        {
            Bowler = bowler,
            Tournament = tournament
        });
        await _dbContext.SaveChangesAsync(ct);

        var handler = new GetBowlerTitlesQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(
            new GetBowlerTitlesQuery { BowlerId = bowler.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Titles.ShouldHaveSingleItem();
        var title = result.Value.Titles.Single();
        title.TournamentId.ShouldBe(tournament.Id);
        title.TournamentName.ShouldBe("NEBA Singles");
        title.TournamentDate.ShouldBe(new DateOnly(2024, 10, 5));
        title.TournamentType.ShouldBe(TournamentType.Singles.Name);
    }

    [Fact(DisplayName = "HandleAsync returns only titles belonging to the requested bowler")]
    public async Task HandleAsync_ShouldReturnOnlyTitlesForRequestedBowler_WhenMultipleChampionRecordsExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var bowler = BowlerFactory.Create();
        var otherBowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler, otherBowler], ct);

        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowlerTournament = TournamentFactory.Create(seasonId: season.Id);
        var otherTournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddRangeAsync([bowlerTournament, otherTournament], ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.HistoricalTournamentChampions.AddRangeAsync(
            new HistoricalTournamentChampion { Bowler = bowler, Tournament = bowlerTournament },
            new HistoricalTournamentChampion { Bowler = otherBowler, Tournament = otherTournament });
        await _dbContext.SaveChangesAsync(ct);

        var handler = new GetBowlerTitlesQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(
            new GetBowlerTitlesQuery { BowlerId = bowler.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Titles.ShouldHaveSingleItem();
        result.Value.Titles.Single().TournamentId.ShouldBe(bowlerTournament.Id);
    }
}