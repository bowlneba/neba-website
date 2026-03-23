using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("Awards")]
[Collection<PostgreSqlFixture>]
public sealed class AwardQueriesTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly AwardQueries _queries;

    public AwardQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = new AppDbContext(fixture.CreateDbContextOptions());
        _queries = new AwardQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetAllHighBlockAwardsAsync returns all High Block awards with correct data")]
    public async Task GetAllHighBlockAwardsAsync_ShouldReturnAllHighBlockAwardsWithCorrectData()
    {
        // Arrange
        const int seed = 77;
        var bowlers = BowlerFactory.Bogus(1000, seed);
        await _dbContext.Bowlers.AddRangeAsync(bowlers, TestContext.Current.CancellationToken);

        var seasons = SeasonFactory.Bogus(10, [.. bowlers.Select(bowler => bowler.Id)], seed);
        await _dbContext.Seasons.AddRangeAsync(seasons, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var highBlockAwards = await _queries.GetAllHighBlockAwardsAsync(TestContext.Current.CancellationToken);

        // Assert
        await Verify(highBlockAwards);
    }
}