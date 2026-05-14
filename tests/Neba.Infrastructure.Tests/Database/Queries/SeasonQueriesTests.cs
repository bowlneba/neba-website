using Neba.Api.Database;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("Seasons")]
[Collection<PostgreSqlFixture>]
public sealed class SeasonQueriesTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly SeasonQueries _queries;

    public SeasonQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = fixture.CreateDbContext();
        _queries = new SeasonQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetAllAsync returns all seasons ordered by start date descending")]
    public async Task GetAllAsync_ReturnsAllSeasonsOrderedByStartDateDescending()
    {
        // Arrange
        const int seed = 42;
        var seasons = SeasonFactory.Bogus(5, seed: seed).ToList();

        await _dbContext.Seasons.AddRangeAsync(seasons, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(5);

        // Verify ordered by start date descending
        var resultList = result.ToList();
        for (int i = 0; i < resultList.Count - 1; i++)
        {
            resultList[i].StartDate.ShouldBeGreaterThanOrEqualTo(resultList[i + 1].StartDate);
        }

        await Verify(resultList);
    }

    [Fact(DisplayName = "GetAllAsync returns empty collection when no seasons exist")]
    public async Task GetAllAsync_ReturnsEmptyCollection_WhenNoSeasonsExist()
    {
        // Act
        var result = await _queries.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetAllAsync includes all season properties in result")]
    public async Task GetAllAsync_IncludesAllSeasonProperties()
    {
        // Arrange
        var season = SeasonFactory.Create(
            description: "Test Season 2026",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31),
            complete: true);

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldHaveSingleItem();
        var seasonDto = result.First();
        seasonDto.Id.ShouldBe(season.Id);
        seasonDto.Description.ShouldBe("Test Season 2026");
        seasonDto.StartDate.ShouldBe(new DateOnly(2026, 1, 1));
        seasonDto.EndDate.ShouldBe(new DateOnly(2026, 12, 31));
    }
}