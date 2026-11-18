using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.ListSeries;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Series.ListSeries;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class ListSeriesQueryHandlerTests(AppDbContextFixture fixture)
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

    private ListSeriesQueryHandler CreateHandler(DateTimeOffset now, IFileStorageService? storage = null)
        => new(_dbContext, storage ?? new Mock<IFileStorageService>(MockBehavior.Strict).Object, new FakeTimeProvider(now));

    private static DateTimeOffset At(int year, int month, int day)
        => new(year, month, day, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "HandleAsync should return an empty collection when there are no Series")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenThereAreNoSeries()
    {
        // Arrange
        var handler = CreateHandler(At(2025, 3, 1));

        // Act
        var result = await handler.HandleAsync(new ListSeriesQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return the Stage counts, lock state and season for each Series")]
    public async Task HandleAsync_ShouldReturnCountsLockAndSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "Open");
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 6, 1), ct, name: "Locked", status: StageStatus.Computed);
        var handler = CreateHandler(At(2025, 1, 1));

        // Act
        var result = (await handler.HandleAsync(new ListSeriesQuery(), ct)).ToList();

        // Assert
        result.Select(s => s.Name).ShouldBe(["Locked", "Open"]);
        var open = result.Single(s => s.Name == "Open");
        open.QualifyingStageCount.ShouldBe(1);
        open.SurvivorStageCount.ShouldBe(1);
        open.PlacementStageCount.ShouldBe(1);
        open.IsLocked.ShouldBeFalse();
        open.SeasonDescription.ShouldBe("2025 Season");
        open.SeasonStartDate.ShouldBe(new DateOnly(2025, 1, 1));
        open.SeasonEndDate.ShouldBe(new DateOnly(2025, 12, 31));
        result.Single(s => s.Name == "Locked").IsLocked.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should list the newest season first")]
    public async Task HandleAsync_ShouldListNewestSeasonFirst()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var older = await SeriesSeed.AddSeasonAsync(_dbContext, 2024, ct);
        var newer = await SeriesSeed.AddSeasonAsync(_dbContext, 2026, ct);
        await SeriesSeed.AddSeriesAsync(_dbContext, older, new DateOnly(2024, 2, 1), ct, name: "Old");
        await SeriesSeed.AddSeriesAsync(_dbContext, newer, new DateOnly(2026, 2, 1), ct, name: "New");
        var handler = CreateHandler(At(2026, 1, 1));

        // Act
        var result = await handler.HandleAsync(new ListSeriesQuery(), ct);

        // Assert
        result.Select(s => s.Name).ShouldBe(["New", "Old"]);
    }

    [Fact(DisplayName = "HandleAsync should set the next Stage to the first unfinished Stage by order")]
    public async Task HandleAsync_ShouldSetNextStage_ToFirstUnfinishedStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "S");
        // Qualifier 2/1, Survivor 3/3, Placement 4/2. On 3/1 the qualifier is over and the survivor is next.
        var handler = CreateHandler(At(2025, 3, 1));

        // Act
        var result = await handler.HandleAsync(new ListSeriesQuery(), ct);

        // Assert
        var next = result.Single().NextStage;
        next.ShouldNotBeNull();
        next.Kind.ShouldBe("Survivor");
        next.TournamentName.ShouldBe("S Survivor");
        next.TournamentStartDate.ShouldBe(new DateOnly(2025, 3, 3));
    }

    [Theory(DisplayName = "HandleAsync should count a tournament that ended up to a day before today as still upcoming")]
    [InlineData(10, true)]
    [InlineData(9, true)]
    [InlineData(8, false)]
    public async Task HandleAsync_ShouldApplyOneDayCutoff(int endDay, bool expectedNext)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var tournament = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Only", new DateOnly(2025, 3, endDay), null, ct);
        var series = Neba.TestFactory.Series.SeriesFactory.CreateFor(season, [(tournament, StageKind.Qualifying, 8, null, null)]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);
        var handler = CreateHandler(At(2025, 3, 10));

        // Act
        var result = await handler.HandleAsync(new ListSeriesQuery(), ct);

        // Assert
        (result.Single().NextStage is not null).ShouldBe(expectedNext);
    }

    [Fact(DisplayName = "HandleAsync should leave the next Stage null when every tournament has ended")]
    public async Task HandleAsync_ShouldLeaveNextStageNull_WhenAllEnded()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);
        var handler = CreateHandler(At(2025, 12, 1));

        // Act
        var result = await handler.HandleAsync(new ListSeriesQuery(), ct);

        // Assert
        result.Single().NextStage.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should build the logo URL only for a Series that has a logo")]
    public async Task HandleAsync_ShouldBuildLogoUrl_OnlyWhenSeriesHasLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var logo = StoredFileFactory.Create(container: "series", path: "logo.png");
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "A With", logo: logo);
        await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 6, 1), ct, name: "B Without");

        var uri = new Uri("https://storage.example.com/series/logo.png");
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.GetBlobUri("series", "logo.png")).Returns(uri);
        var handler = CreateHandler(At(2025, 1, 1), storage.Object);

        // Act
        var result = (await handler.HandleAsync(new ListSeriesQuery(), ct)).ToList();

        // Assert
        result.Single(s => s.Name == "A With").LogoUrl.ShouldBe(uri);
        result.Single(s => s.Name == "B Without").LogoUrl.ShouldBeNull();
        storage.Verify(s => s.GetBlobUri("series", "logo.png"), Times.Once);
    }
}