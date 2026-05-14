using Microsoft.EntityFrameworkCore;

using Neba.Domain.Seasons;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Repositories;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;

namespace Neba.Infrastructure.Tests.Database.Repositories;

[IntegrationTest]
[Component("Seasons")]
[Collection<PostgreSqlFixture>]
public sealed class SeasonRepositoryTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly SeasonRepository _sut;

    public SeasonRepositoryTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = fixture.CreateDbContext();
        _sut = new SeasonRepository(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetSeasonByIdAsync returns season when it exists")]
    public async Task GetSeasonByIdAsync_ShouldReturnSeason_WhenSeasonExists()
    {
        // Arrange
        var season = SeasonFactory.Create(description: "2026 Season");
        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetSeasonByIdAsync(season.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(season.Id);
        result.Description.ShouldBe(season.Description);
    }

    [Fact(DisplayName = "GetSeasonByIdAsync returns null when season does not exist")]
    public async Task GetSeasonByIdAsync_ShouldReturnNull_WhenSeasonDoesNotExist()
    {
        // Act
        var result = await _sut.GetSeasonByIdAsync(SeasonId.New(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetSeasonByIdAsync tracks entity when tracking is enabled")]
    public async Task GetSeasonByIdAsync_ShouldTrackEntity_WhenTrackChangesIsTrue()
    {
        // Arrange
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetSeasonByIdAsync(
            season.Id,
            trackChanges: true,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();

        _dbContext.ChangeTracker.Entries<Season>().Count().ShouldBe(1);
        _dbContext.Entry(result).State.ShouldBe(EntityState.Unchanged);
    }

    [Fact(DisplayName = "GetSeasonByIdAsync does not track entity when tracking is disabled")]
    public async Task GetSeasonByIdAsync_ShouldNotTrackEntity_WhenTrackChangesIsFalse()
    {
        // Arrange
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetSeasonByIdAsync(
            season.Id,
            trackChanges: false,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        _dbContext.ChangeTracker.Entries<Season>().ShouldBeEmpty();
    }
}