using Neba.Domain.BowlingCenters;
using Neba.Infrastructure.BowlingCenters;
using Neba.Infrastructure.Database;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Geography;
using Neba.TestFactory.Infrastructure;

namespace Neba.Infrastructure.Tests.BowlingCenters;

[IntegrationTest]
[Component("BowlingCenter")]
[Collection<PostgreSqlFixture>]
public sealed class BowlingCenterQueriesTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private readonly AppDbContext _dbContext;
    private readonly BowlingCenterQueries _sut;

    public BowlingCenterQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
        _dbContext = new AppDbContext(fixture.CreateDbContextOptions());
        _sut = new BowlingCenterQueries(_dbContext);
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _fixture.ResetAsync();
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
        var result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(expectedOpenCount);
        result.ShouldAllBe(c => c.Status == BowlingCenterStatus.Open);
        result.ShouldNotContain(c => c.CertificationNumber == "00001");
        await Verify(result.OrderBy(c => c.CertificationNumber));
    }
}