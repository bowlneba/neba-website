using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("Tournaments")]
[Collection<PostgreSqlFixture>]
public sealed class TournamentQueriesTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly TournamentQueries _sut;

    public TournamentQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = fixture.CreateDbContext();
        _sut = new TournamentQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetTournamentCountForSeasonAsync returns count for tournaments in requested season only")]
    public async Task GetTournamentCountForSeasonAsync_ShouldReturnCountForRequestedSeasonOnly()
    {
        // Arrange
        var requestedSeason = SeasonFactory.Create(description: "Requested Season");
        var otherSeason = SeasonFactory.Create(description: "Other Season");

        var requestedSeasonTournaments = new[]
        {
            TournamentFactory.Create(name: "Tournament A", seasonId: requestedSeason.Id),
            TournamentFactory.Create(name: "Tournament B", seasonId: requestedSeason.Id),
            TournamentFactory.Create(name: "Tournament C", seasonId: requestedSeason.Id)
        };

        var otherSeasonTournament = TournamentFactory.Create(
            name: "Other Tournament",
            seasonId: otherSeason.Id);

        await _dbContext.Seasons.AddRangeAsync(
            [requestedSeason, otherSeason],
            TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync(requestedSeasonTournaments, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(otherSeasonTournament, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentCountForSeasonAsync(requestedSeason.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(3);
    }

    [Fact(DisplayName = "GetTournamentCountForSeasonAsync returns zero when requested season has no tournaments")]
    public async Task GetTournamentCountForSeasonAsync_ShouldReturnZero_WhenRequestedSeasonHasNoTournaments()
    {
        // Arrange
        var requestedSeason = SeasonFactory.Create(description: "Requested Season");
        var otherSeason = SeasonFactory.Create(description: "Other Season");

        var tournamentsInOtherSeason = new[]
        {
            TournamentFactory.Create(name: "Other Tournament A", seasonId: otherSeason.Id),
            TournamentFactory.Create(name: "Other Tournament B", seasonId: otherSeason.Id)
        };

        await _dbContext.Seasons.AddRangeAsync(
            [requestedSeason, otherSeason],
            TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync(tournamentsInOtherSeason, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentCountForSeasonAsync(requestedSeason.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(0);
    }
}