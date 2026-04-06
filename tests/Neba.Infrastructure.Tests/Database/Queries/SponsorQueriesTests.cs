using Neba.Domain.BowlingCenters;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Geography;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Sponsors;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("Sponsors")]
[Collection<PostgreSqlFixture>]
public sealed class SponsorQueriesTests 
    : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly SponsorQueries _queries;

    public SponsorQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = fixture.CreateDbContext();
        _queries = new SponsorQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetActiveSponsorsAsync returns only current sponsors")]
    public async Task GetActiveSponsorsAsync_ReturnsOnlyCurrentSponsors()
    {
        // Arrange
        const int seed = 90;
        var sponsors = SponsorFactory.Bogus(10, seed);

        // If this assertion fails, seed 90 produced no current sponsors — choose a different seed
        var expectedCurrentCount = sponsors.Count(s => s.IsCurrentSponsor);
        expectedCurrentCount.ShouldBeGreaterThan(0);

        var oldSponsor = SponsorFactory.Create(
            name: "Previous Sponsor",
            isCurrentSponsor: false);

        await _dbContext.Sponsors.AddRangeAsync(sponsors, TestContext.Current.CancellationToken);
        await _dbContext.Sponsors.AddAsync(oldSponsor, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _queries.GetActiveSponsorsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(expectedCurrentCount);
        result.ShouldAllBe(s => s.IsCurrentSponsor);
        result.ShouldNotContain(s => s.Name == "Previous Sponsor");
        
        await Verify(result);
    }
}