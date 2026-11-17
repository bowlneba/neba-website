using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Series;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesFinaleChecksTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task<BowlingCenter> AddCenterAsync(BowlingCenterStatus status)
    {
        var center = BowlingCenterFactory.Create(status: status);
        _dbContext.BowlingCenters.Add(center);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return center;
    }

    [Fact(DisplayName = "EnsureBowlingCenterUsableAsync should succeed without a query when there is no Finale")]
    public async Task Ensure_ShouldSucceed_WhenThereIsNoFinale()
    {
        // Arrange & Act
        var result = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(_dbContext, null, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureBowlingCenterUsableAsync should succeed when the Finale has no center")]
    public async Task Ensure_ShouldSucceed_WhenFinaleHasNoCenter()
    {
        // Arrange
        var finale = SeriesFinaleDefinitionFactory.Create();

        // Act
        var result = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(_dbContext, finale, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureBowlingCenterUsableAsync should succeed for an Open center")]
    public async Task Ensure_ShouldSucceed_ForOpenCenter()
    {
        // Arrange
        var center = await AddCenterAsync(BowlingCenterStatus.Open);
        var finale = SeriesFinaleDefinitionFactory.Create(bowlingCenterId: center.CertificationNumber);

        // Act
        var result = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(_dbContext, finale, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "EnsureBowlingCenterUsableAsync should fail for an unknown center")]
    public async Task Ensure_ShouldFail_ForUnknownCenter()
    {
        // Arrange
        var finale = SeriesFinaleDefinitionFactory.Create(bowlingCenterId: CertificationNumberFactory.Create("99999"));

        // Act
        var result = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(_dbContext, finale, null, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Finale.BowlingCenter.NotAvailable");
    }

    [Theory(DisplayName = "EnsureBowlingCenterUsableAsync should fail for a Closed or Uncertified center")]
    [InlineData("Closed")]
    [InlineData("Uncertified")]
    public async Task Ensure_ShouldFail_ForCenterThatIsNotOpen(string statusName)
    {
        // Arrange
        var center = await AddCenterAsync(BowlingCenterStatus.FromName(statusName));
        var finale = SeriesFinaleDefinitionFactory.Create(bowlingCenterId: center.CertificationNumber);

        // Act
        var result = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(_dbContext, finale, null, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Finale.BowlingCenter.NotAvailable");
    }

    [Fact(DisplayName = "EnsureBowlingCenterUsableAsync should succeed for the center the Finale already has, even when it is now Closed")]
    public async Task Ensure_ShouldSucceed_ForCurrentCenterThatClosed()
    {
        // Arrange
        var center = await AddCenterAsync(BowlingCenterStatus.Closed);
        var finale = SeriesFinaleDefinitionFactory.Create(bowlingCenterId: center.CertificationNumber);

        // Act
        var result = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(
            _dbContext, finale, center.CertificationNumber, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }
}
