using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.CreateSeries;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

namespace Neba.Api.Tests.Features.Series.CreateSeries;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class CreateSeriesCommandHandlerTests(AppDbContextFixture fixture)
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

    private CreateSeriesCommandHandler CreateHandler(Mock<ZiggyCreatures.Caching.Fusion.IFusionCache>? cache = null)
        => new(_dbContext, (cache ?? SeriesCache.Evicting()).Object);

    private static CreateSeriesCommand Command(
        SeasonId seasonId,
        IReadOnlyList<SeriesStageDefinition> stages,
        SeriesFinaleDefinition? finale = null,
        Neba.Api.Features.Storage.Domain.StoredFile? logo = null)
        => new() { Name = "Series", SeasonId = seasonId, Stages = stages, Finale = finale, Logo = logo };

    private async Task<(Season Season, Neba.Api.Features.Tournaments.Domain.Tournament Tournament)> SeedAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var tournament = await SeriesSeed.AddTournamentAsync(_dbContext, season, "T", new DateOnly(2025, 3, 1), null, ct);
        return (season, tournament);
    }

    [Fact(DisplayName = "HandleAsync should return SeasonNotFound when the season does not exist")]
    public async Task HandleAsync_ShouldReturnSeasonNotFound_WhenSeasonIsMissing()
    {
        // Arrange
        var command = Command(SeasonId.New(), []);

        // Act
        var result = await CreateHandler().HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Season.NotFound");
    }

    [Fact(DisplayName = "HandleAsync should save the Series and return its id")]
    public async Task HandleAsync_ShouldSaveSeries_WhenValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await SeedAsync();
        var stage = SeriesStageDefinitionFactory.Create(tournamentId: tournament.Id);

        // Act
        var result = await CreateHandler().HandleAsync(Command(season.Id, [stage]), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var saved = await _dbContext.Series.AsNoTracking().Include(s => s.Stages).SingleAsync(ct);
        saved.Id.ShouldBe(result.Value);
        saved.Stages.Single().TournamentId.ShouldBe(tournament.Id);
    }

    [Fact(DisplayName = "HandleAsync should clear the tournament cache tag once after a successful save")]
    public async Task HandleAsync_ShouldEvictTournamentCache_AfterSave()
    {
        // Arrange
        var (season, tournament) = await SeedAsync();
        var cache = SeriesCache.Evicting();

        // Act
        var result = await CreateHandler(cache).HandleAsync(
            Command(season.Id, [SeriesStageDefinitionFactory.Create(tournamentId: tournament.Id)]), TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        SeriesCache.VerifyEvictedOnce(cache);
    }

    [Fact(DisplayName = "HandleAsync should not touch the tournament cache when the save fails validation")]
    public async Task HandleAsync_ShouldNotEvictTournamentCache_WhenValidationFails()
    {
        // Arrange
        var (season, tournament) = await SeedAsync();
        var broken = SeriesStageDefinitionFactory.Create(tournamentId: tournament.Id, advancingCount: null);

        // Act (an untouched strict cache fails the test if it is called)
        var result = await CreateHandler(SeriesCache.Untouched()).HandleAsync(Command(season.Id, [broken]), TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should return the domain errors and save nothing")]
    public async Task HandleAsync_ShouldReturnDomainErrors_WhenStageBreaksARule()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await SeedAsync();
        var stage = SeriesStageDefinitionFactory.Create(tournamentId: tournament.Id, advancingCount: null);

        // Act
        var result = await CreateHandler().HandleAsync(Command(season.Id, [stage]), ct);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.AdvancingCountRequired");
        (await _dbContext.Series.CountAsync(ct)).ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should return a Conflict when a tournament is already a Stage in another Series")]
    public async Task HandleAsync_ShouldReturnConflict_WhenTournamentIsInAnotherSeries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var other = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "Other");
        var stage = SeriesStageDefinitionFactory.Create(tournamentId: other.Stages.First().TournamentId);

        // Act
        var result = await CreateHandler().HandleAsync(Command(season.Id, [stage]), ct);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.TournamentInAnotherSeries");
        result.Errors.Single().Type.ShouldBe(ErrorOr.ErrorType.Conflict);
    }

    [Fact(DisplayName = "HandleAsync should return FinaleBowlingCenterNotAvailable for an unknown center")]
    public async Task HandleAsync_ShouldReturnCenterError_WhenFinaleCenterIsUnknown()
    {
        // Arrange
        var (season, tournament) = await SeedAsync();
        var finale = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1), bowlingCenterId: CertificationNumberFactory.Create("99999"));

        // Act
        var result = await CreateHandler().HandleAsync(
            Command(season.Id, [SeriesStageDefinitionFactory.Create(tournamentId: tournament.Id)], finale), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Finale.BowlingCenter.NotAvailable");
    }

    [Fact(DisplayName = "HandleAsync should return LogoNotUploaded when the logo was not staged")]
    public async Task HandleAsync_ShouldReturnLogoNotUploaded_WhenLogoWasNotStaged()
    {
        // Arrange
        var (season, tournament) = await SeedAsync();
        var command = Command(season.Id, [SeriesStageDefinitionFactory.Create(tournamentId: tournament.Id)], logo: StoredFileFactory.Create());

        // Act
        var result = await CreateHandler().HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Logo.NotUploaded");
    }

    [Fact(DisplayName = "HandleAsync should save the logo and remove the claimed pending upload")]
    public async Task HandleAsync_ShouldClaimStagedLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await SeedAsync();
        var logo = StoredFileFactory.Create(container: "series", path: "logo.png");
        _dbContext.PendingUploads.Add(PendingUploadFactory.Create(container: "series", path: "logo.png"));
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(
            Command(season.Id, [SeriesStageDefinitionFactory.Create(tournamentId: tournament.Id)], logo: logo), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Logo!.Path.ShouldBe("logo.png");
        (await _dbContext.PendingUploads.CountAsync(ct)).ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should save a Series with a Finale and an Open host center")]
    public async Task HandleAsync_ShouldSaveFinale_WithOpenCenter()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await SeedAsync();
        var center = BowlingCenterFactory.Create(status: BowlingCenterStatus.Open);
        _dbContext.BowlingCenters.Add(center);
        await _dbContext.SaveChangesAsync(ct);
        var finale = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1), bowlingCenterId: center.CertificationNumber);

        // Act
        var result = await CreateHandler().HandleAsync(
            Command(season.Id, [SeriesStageDefinitionFactory.Create(tournamentId: tournament.Id)], finale), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var saved = await _dbContext.Series.AsNoTracking().SingleAsync(ct);
        saved.Finale!.BowlingCenterId.ShouldBe(center.CertificationNumber);
    }
}
