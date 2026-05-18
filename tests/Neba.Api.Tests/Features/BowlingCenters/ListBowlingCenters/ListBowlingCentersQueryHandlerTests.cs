using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.BowlingCenters.ListBowlingCenters;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Features.BowlingCenters.ListBowlingCenters;

[IntegrationTest]
[Component("BowlingCenters")]
[Collection<PostgreSqlFixture>]
public sealed class ListBowlingCentersQueryHandlerTests(PostgreSqlFixture fixture)
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

    [Fact(DisplayName = "HandleAsync returns empty collection when no bowling centers exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoBowlingCentersExist()
    {
        var handler = new ListBowlingCentersQueryHandler(_dbContext);

        var result = await handler.HandleAsync(
            new ListBowlingCentersQuery(),
            TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns bowling center with correct fields when data exists")]
    public async Task HandleAsync_ShouldReturnCenter_WithCorrectFields_WhenDataExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var address = AddressFactory.CreateUsAddress(
            street: "100 Bowling Dr",
            city: "Hartford",
            coordinates: AddressFactory.ValidCoordinates);
        var center = BowlingCenterFactory.Create(
            certificationNumber: CertificationNumberFactory.Create("99999"),
            name: "Test Lanes",
            address: address);
        await _dbContext.BowlingCenters.AddAsync(center, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListBowlingCentersQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListBowlingCentersQuery(), ct);

        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.Name.ShouldBe("Test Lanes");
        dto.CertificationNumber.ShouldBe("99999");
        dto.Address.City.ShouldBe("Hartford");
        dto.Address.Latitude.ShouldBe(AddressFactory.ValidCoordinates.Latitude);
        dto.Address.Longitude.ShouldBe(AddressFactory.ValidCoordinates.Longitude);
    }

    [Fact(DisplayName = "HandleAsync returns only open bowling centers when multiple exist")]
    public async Task HandleAsync_ShouldReturnOnlyOpenCenters_WhenMultipleExist()
    {
        var ct = TestContext.Current.CancellationToken;
        const int seed = 21;
        var centers = BowlingCenterFactory.Bogus(10, seed);
        await _dbContext.BowlingCenters.AddRangeAsync(centers, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListBowlingCentersQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListBowlingCentersQuery(), ct);

        var expectedCount = centers.Count(c => c.Status == BowlingCenterStatus.Open);
        result.Count.ShouldBe(expectedCount);
        foreach (var dto in result)
        {
            dto.Name.ShouldNotBeNullOrWhiteSpace();
            dto.CertificationNumber.ShouldNotBeNullOrWhiteSpace();
            dto.Address.ShouldNotBeNull();
            dto.Status.ShouldBe(BowlingCenterStatus.Open.Name);
        }
    }

    [Fact(DisplayName = "HandleAsync excludes closed and uncertified bowling centers")]
    public async Task HandleAsync_ShouldExcludeClosedAndUncertifiedCenters()
    {
        var ct = TestContext.Current.CancellationToken;
        var openCenter = BowlingCenterFactory.Create(
            certificationNumber: CertificationNumberFactory.Create("11111"),
            status: BowlingCenterStatus.Open);
        var closedCenter = BowlingCenterFactory.Create(
            certificationNumber: CertificationNumberFactory.Create("22222"),
            status: BowlingCenterStatus.Closed);
        var uncertifiedCenter = BowlingCenterFactory.Create(
            certificationNumber: CertificationNumberFactory.Create("33333"),
            status: BowlingCenterStatus.Uncertified);

        await _dbContext.BowlingCenters.AddRangeAsync([openCenter, closedCenter, uncertifiedCenter], ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListBowlingCentersQueryHandler(_dbContext);

        var result = await handler.HandleAsync(new ListBowlingCentersQuery(), ct);

        result.ShouldHaveSingleItem();
        result.Single().CertificationNumber.ShouldBe("11111");
    }
}