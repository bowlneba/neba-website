using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("Bowlers")]
[Collection<PostgreSqlFixture>]
public sealed class BowlerQueriesTests
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly BowlerQueries _sut;

    public BowlerQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = fixture.CreateDbContext();
        _sut = new BowlerQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetBowlerIdByLegacyIdAsync returns only bowlers with a LegacyId")]
    public async Task GetBowlerIdByLegacyIdAsync_ShouldReturnOnlyBowlersWithLegacyId()
    {
        // Arrange
        var bowlerWithLegacyId = BowlerFactory.Create(legacyId: 1001);
        var bowlerWithoutLegacyId = BowlerFactory.Create(legacyId: null);

        await _dbContext.Bowlers.AddRangeAsync(
            [bowlerWithLegacyId, bowlerWithoutLegacyId],
            TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetBowlerIdByLegacyIdAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldNotContainKey(0);
        result.ShouldContainKey(1001);
        result[1001].ShouldBe(bowlerWithLegacyId.Id);
    }

    [Fact(DisplayName = "GetBowlerIdByLegacyIdAsync maps LegacyId to correct BowlerId")]
    public async Task GetBowlerIdByLegacyIdAsync_ShouldMapLegacyIdToCorrectBowlerId()
    {
        // Arrange
        var bowlerA = BowlerFactory.Create(legacyId: 2001);
        var bowlerB = BowlerFactory.Create(legacyId: 2002);

        await _dbContext.Bowlers.AddRangeAsync(
            [bowlerA, bowlerB],
            TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetBowlerIdByLegacyIdAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result[2001].ShouldBe(bowlerA.Id);
        result[2002].ShouldBe(bowlerB.Id);
    }

    [Fact(DisplayName = "GetBowlerIdByLegacyIdAsync returns empty dictionary when no bowlers have a LegacyId")]
    public async Task GetBowlerIdByLegacyIdAsync_ShouldReturnEmptyDictionary_WhenNoBowlersHaveLegacyId()
    {
        // Arrange
        var bowlers = new[]
        {
            BowlerFactory.Create(legacyId: null),
            BowlerFactory.Create(legacyId: null)
        };

        await _dbContext.Bowlers.AddRangeAsync(bowlers, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetBowlerIdByLegacyIdAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetBowlerIdByLegacyIdAsync returns empty dictionary when no bowlers exist")]
    public async Task GetBowlerIdByLegacyIdAsync_ShouldReturnEmptyDictionary_WhenNoBowlersExist()
    {
        // Act
        var result = await _sut.GetBowlerIdByLegacyIdAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }
}