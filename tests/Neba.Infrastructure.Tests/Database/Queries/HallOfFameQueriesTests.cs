using Neba.Domain.Bowlers;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("HallOfFame")]
[Collection<PostgreSqlFixture>]
public sealed class HallOfFameQueriesTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private readonly AppDbContext _dbContext;
    private readonly HallOfFameQueries _sut;

    public HallOfFameQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
        _dbContext = new AppDbContext(fixture.CreateDbContextOptions());
        _sut = new HallOfFameQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetAllAsync returns all hall of fame entries")]
    public async Task GetAllAsync_ShouldReturnAllHallOfFameInductions()
    {
        // Arrange
        const int seed = 50;
        var bowlers = BowlerFactory.Bogus(50, seed);
        var bowlerIdPool = UniquePool.Create(bowlers.Select(b => b.Id), seed);
        var inductions = HallOfFameInductionFactory.Bogus(5, bowlerIdPool, seed);

        var inductionWithPhoto = HallOfFameInductionFactory.Create(
            bowlerId: bowlerIdPool.GetNextNullable(),
            photo: StoredFileFactory.Bogus(1, seed: seed).Single()
        );

        var inductionWithoutPhoto = HallOfFameInductionFactory.Create(
            bowlerId: bowlerIdPool.GetNextNullable(),
            photo: null
        );

        var bowlerWithPhoto = bowlers.Single(b => b.Id == inductionWithPhoto.BowlerId);
        var bowlerWithoutPhoto = bowlers.Single(b => b.Id == inductionWithoutPhoto.BowlerId);

        await _dbContext.Bowlers.AddRangeAsync(bowlers, TestContext.Current.CancellationToken);
        await _dbContext.HallOfFameInductions.AddRangeAsync([.. inductions, inductionWithPhoto, inductionWithoutPhoto], TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(inductions.Count + 2);
        result.ShouldContain(r => r.BowlerName == bowlerWithPhoto.Name && r.PhotoContainer != null);
        result.ShouldContain(r => r.BowlerName == bowlerWithoutPhoto.Name && r.PhotoContainer == null);

        await Verify(result);
    }
}