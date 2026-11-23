using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeries;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Series.GetSeries;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class GetSeriesQueryHandlerTests(AppDbContextFixture fixture)
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

    private GetSeriesQueryHandler CreateHandler(IFileStorageService? storage = null)
        => new(_dbContext, storage ?? new Mock<IFileStorageService>(MockBehavior.Strict).Object);

    [Fact(DisplayName = "HandleAsync should return NotFound when the Series does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesDoesNotExist()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new GetSeriesQuery { Id = SeriesId.New() }, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.NotFound");
    }

    [Fact(DisplayName = "HandleAsync should return the Series, its season and ordered Stages with tournament details")]
    public async Task HandleAsync_ShouldReturnSeriesWithOrderedStages()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "VH");
        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(new GetSeriesQuery { Id = series.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var dto = result.Value;
        dto.Name.ShouldBe("VH");
        dto.SeasonDescription.ShouldBe("2025 Season");
        dto.IsLocked.ShouldBeFalse();
        dto.Finale.ShouldBeNull();
        dto.LogoUrl.ShouldBeNull();
        dto.Stages.Select(s => s.Order).ShouldBe([1, 2, 3]);
        dto.Stages.Select(s => s.Kind).ShouldBe(["Qualifying", "Survivor", "Placement"]);
        dto.Stages.Select(s => s.AdvancingCount).ShouldBe([8, 20, null]);
        dto.Stages.Select(s => s.Status).ShouldAllBe(status => status == "Pending");
        dto.Stages.Select(s => s.TournamentName).ShouldBe(["VH Qualifier", "VH Survivor", "VH Placement"]);
        dto.Stages.Select(s => s.TeamSize).ShouldAllBe(size => size == 1);
        var survivor = dto.Stages.Single(s => s.Kind == "Survivor");
        survivor.SquadDate.ShouldBe(new DateOnly(2025, 3, 3));
        survivor.SquadStartTime.ShouldBe(new TimeOnly(9, 0));
    }

    [Fact(DisplayName = "HandleAsync should report the Series as locked when a Stage has left pending")]
    public async Task HandleAsync_ShouldReportLocked_WhenStageIsNotPending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, status: StageStatus.Official);

        // Act
        var result = await CreateHandler().HandleAsync(new GetSeriesQuery { Id = series.Id }, ct);

        // Assert
        result.Value.IsLocked.ShouldBeTrue();
        result.Value.Stages.First().Status.ShouldBe("Official");
    }

    [Fact(DisplayName = "HandleAsync should return the Finale with its host center details")]
    public async Task HandleAsync_ShouldReturnFinaleWithHostCenter()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var center = BowlingCenterFactory.Create();
        _dbContext.BowlingCenters.Add(center);
        await _dbContext.SaveChangesAsync(ct);

        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var finale = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1), bowlingCenterId: center.CertificationNumber);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, finale: finale);

        // Act
        var result = await CreateHandler().HandleAsync(new GetSeriesQuery { Id = series.Id }, ct);

        // Assert
        var dto = result.Value.Finale;
        dto.ShouldNotBeNull();
        dto.FinalistCount.ShouldBe(4);
        dto.Date.ShouldBe(new DateOnly(2025, 12, 1));
        dto.StartTime.ShouldBe(new TimeOnly(14, 0));
        dto.BowlingCenterId.ShouldBe(center.CertificationNumber.Value);
        dto.BowlingCenterName.ShouldBe(center.Name);
        dto.BowlingCenterCity.ShouldBe(center.Address.City);
        dto.BowlingCenterState.ShouldBe(center.Address.Region);
    }

    [Fact(DisplayName = "HandleAsync should return a Finale with no host center fields when none is set")]
    public async Task HandleAsync_ShouldReturnFinaleWithoutCenter_WhenNoneIsSet()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var finale = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1));
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, finale: finale);

        // Act
        var result = await CreateHandler().HandleAsync(new GetSeriesQuery { Id = series.Id }, ct);

        // Assert
        result.Value.Finale!.BowlingCenterId.ShouldBeNull();
        result.Value.Finale.BowlingCenterName.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return the logo URL and the stored file fields")]
    public async Task HandleAsync_ShouldReturnLogoFields()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var logo = StoredFileFactory.Create(container: "series", path: "logo.png", contentType: "image/png", sizeInBytes: 99);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, logo: logo);

        var uri = new Uri("https://storage.example.com/series/logo.png");
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.GetBlobUri("series", "logo.png")).Returns(uri);

        // Act
        var result = await CreateHandler(storage.Object).HandleAsync(new GetSeriesQuery { Id = series.Id }, ct);

        // Assert
        var dto = result.Value;
        dto.LogoUrl.ShouldBe(uri);
        dto.LogoContainer.ShouldBe("series");
        dto.LogoPath.ShouldBe("logo.png");
        dto.LogoContentType.ShouldBe("image/png");
        dto.LogoSizeInBytes.ShouldBe(99);
    }

    [Fact(DisplayName = "HandleAsync should report the team size of a team tournament Stage")]
    public async Task HandleAsync_ShouldReportTeamSize_ForTeamTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var team = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Trios", new DateOnly(2025, 2, 21), null, ct,
            Neba.Api.Features.Tournaments.Domain.TournamentType.Trios);
        var series = SeriesFactory.CreateFor(season, [(team, StageKind.Qualifying, 2, null, null)]);
        _dbContext.Series.Add(series);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new GetSeriesQuery { Id = series.Id }, ct);

        // Assert
        result.Value.Stages.Single().TeamSize.ShouldBe(3);
        result.Value.Stages.Single().TournamentType.ShouldBe("Trios");
    }
}