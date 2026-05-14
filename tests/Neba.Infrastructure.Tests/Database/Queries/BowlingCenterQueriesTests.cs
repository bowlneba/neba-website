using Neba.Api.Database;
using Neba.Domain.BowlingCenters;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Geography;
using Neba.TestFactory.Infrastructure;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("BowlingCenters")]
[Collection<PostgreSqlFixture>]
public sealed class BowlingCenterQueriesTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly BowlingCenterQueries _queries;

    public BowlingCenterQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = fixture.CreateDbContext();
        _queries = new BowlingCenterQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetAllAsync returns only open bowling centers")]
    public async Task GetAllAsync_ReturnsOnlyOpenBowlingCenters()
    {
        // Arrange
        const int seed = 42;
        var centers = BowlingCenterFactory.Bogus(10, seed);

        // If this assertion fails, seed 42 produced no open centers — choose a different seed
        var expectedOpenCount = centers.Count(c => c.Status == BowlingCenterStatus.Open);
        expectedOpenCount.ShouldBeGreaterThan(0);

        var closedCenter = BowlingCenterFactory.Create(
            certificationNumber: CertificationNumberFactory.Create("00001"),
            status: BowlingCenterStatus.Closed,
            address: AddressFactory.CreateUsAddress(coordinates: CoordinatesFactory.Create()));

        await _dbContext.BowlingCenters.AddRangeAsync(centers, TestContext.Current.CancellationToken);
        await _dbContext.BowlingCenters.AddAsync(closedCenter, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(expectedOpenCount);
        result.ShouldAllBe(c => c.Status == BowlingCenterStatus.Open.Name);
        result.ShouldNotContain(c => c.CertificationNumber == "00001");
        await Verify(result.OrderBy(c => c.CertificationNumber));
    }
}