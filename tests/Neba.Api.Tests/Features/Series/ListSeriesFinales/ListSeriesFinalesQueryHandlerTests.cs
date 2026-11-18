using Neba.Api.Database;
using Neba.Api.Features.Series.ListSeriesFinales;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Series.ListSeriesFinales;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class ListSeriesFinalesQueryHandlerTests(AppDbContextFixture fixture)
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

    private ListSeriesFinalesQueryHandler CreateHandler(IFileStorageService? storage = null)
        => new(_dbContext, storage ?? new Mock<IFileStorageService>(MockBehavior.Strict).Object);

    [Fact(DisplayName = "HandleAsync should return an empty collection when no Series has a Finale")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoSeriesHasFinale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);

        // Act
        var result = await CreateHandler().HandleAsync(new ListSeriesFinalesQuery(), ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return only Series with a Finale, soonest first by date then start time")]
    public async Task HandleAsync_ShouldReturnOnlyFinales_SoonestFirst()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var late = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 20));
        var earlyPm = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 5), startTime: new TimeOnly(18, 0));
        var earlyAm = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 5), startTime: new TimeOnly(9, 0));
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 1, 1), ct, name: "Late", finale: late);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 3, 1), ct, name: "EarlyPm", finale: earlyPm);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 5, 1), ct, name: "EarlyAm", finale: earlyAm);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 7, 1), ct, name: "None");

        // Act
        var result = await CreateHandler().HandleAsync(new ListSeriesFinalesQuery(), ct);

        // Assert
        result.Select(r => r.SeriesName).ShouldBe(["EarlyAm", "EarlyPm", "Late"]);
    }

    [Fact(DisplayName = "HandleAsync should return the host center name, city and state, and null with no center")]
    public async Task HandleAsync_ShouldReturnHostCenter_AndNullWithNoCenter()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var center = BowlingCenterFactory.Create();
        _dbContext.BowlingCenters.Add(center);
        await _dbContext.SaveChangesAsync(ct);

        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 1, 1), ct, name: "Hosted",
            finale: SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1), bowlingCenterId: center.CertificationNumber));
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 3, 1), ct, name: "Unhosted",
            finale: SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 2)));

        // Act
        var result = (await CreateHandler().HandleAsync(new ListSeriesFinalesQuery(), ct)).ToList();

        // Assert
        var hosted = result.Single(r => r.SeriesName == "Hosted").Finale;
        hosted.BowlingCenterName.ShouldBe(center.Name);
        hosted.BowlingCenterCity.ShouldBe(center.Address.City);
        hosted.BowlingCenterState.ShouldBe(center.Address.Region);
        var unhosted = result.Single(r => r.SeriesName == "Unhosted").Finale;
        unhosted.BowlingCenterId.ShouldBeNull();
        unhosted.BowlingCenterName.ShouldBeNull();
        unhosted.BowlingCenterCity.ShouldBeNull();
        unhosted.BowlingCenterState.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should set the logo URL only when the Series has a logo")]
    public async Task HandleAsync_ShouldSetLogoUrl_OnlyWhenSeriesHasLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var logo = StoredFileFactory.Create(container: "series", path: "logo.png");
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 1, 1), ct, name: "With", logo: logo,
            finale: SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1)));
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 3, 1), ct, name: "Without",
            finale: SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 2)));

        var uri = new Uri("https://storage.example.com/series/logo.png");
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.GetBlobUri("series", "logo.png")).Returns(uri);

        // Act
        var result = (await CreateHandler(storage.Object).HandleAsync(new ListSeriesFinalesQuery(), ct)).ToList();

        // Assert
        result.Single(r => r.SeriesName == "With").LogoUrl.ShouldBe(uri);
        result.Single(r => r.SeriesName == "Without").LogoUrl.ShouldBeNull();
        storage.Verify(s => s.GetBlobUri("series", "logo.png"), Times.Once);
    }
}