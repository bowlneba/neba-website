using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.EditSeries;
using Neba.Api.Security.Infrastructure.Authorization;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series.EditSeries;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class EditSeriesCommandHandlerTests(AppDbContextFixture fixture)
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

    private static IPermissionDenialReader Denial(bool denied, bool expectLookup = true)
    {
        var mock = new Mock<IPermissionDenialReader>(MockBehavior.Strict);
        if (expectLookup)
        {
            mock.Setup(r => r.IsDeniedAsync(It.IsAny<Ulid>(), Neba.Api.Contracts.Security.Permissions.EditSeriesLogo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(denied);
        }

        return mock.Object;
    }

    private EditSeriesCommandHandler CreateHandler(
        IPermissionDenialReader? denial = null,
        IBackgroundJobScheduler? scheduler = null,
        Mock<ZiggyCreatures.Caching.Fusion.IFusionCache>? cache = null)
        => new(
            _dbContext,
            denial ?? Denial(denied: false, expectLookup: false),
            scheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object,
            (cache ?? SeriesCache.Evicting()).Object);

    /// <summary>The Series' current Stages as definitions carrying their ids.</summary>
    private static List<SeriesStageDefinition> Current(SeriesAggregate series)
        => [.. series.Stages.Select(s => SeriesStageDefinitionFactory.Create(
            s.Id, s.Kind, s.TournamentId, s.AdvancingCount, s.SquadDesignation?.Date, s.SquadDesignation?.StartTime))];

    private static EditSeriesCommand Command(
        SeriesAggregate series,
        IReadOnlyList<SeriesStageDefinition>? stages = null,
        string? name = null,
        SeriesFinaleDefinition? finale = null,
        Neba.Api.Features.Storage.Domain.StoredFile? logo = null)
        => new()
        {
            SeriesId = series.Id,
            CallerUserId = Ulid.NewUlid(),
            Name = name ?? series.Name,
            Stages = stages ?? Current(series),
            Finale = finale,
            Logo = logo,
        };

    private async Task<SeriesAggregate> SeedAsync(
        SeriesFinaleDefinition? finale = null,
        Neba.Api.Features.Storage.Domain.StoredFile? logo = null,
        StageStatus? status = null)
    {
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "VH", finale: finale, logo: logo, status: status);
        _dbContext.ChangeTracker.Clear();
        return series;
    }

    [Fact(DisplayName = "HandleAsync should return NotFound when the Series does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenSeriesIsMissing()
    {
        // Arrange
        var series = SeriesFactory.Create();

        // Act
        var result = await CreateHandler().HandleAsync(Command(series), TestContext.Current.CancellationToken);

        // Assert
        result.FirstError.Code.ShouldBe("Series.NotFound");
    }

    [Fact(DisplayName = "HandleAsync should rename an unchanged Series and save")]
    public async Task HandleAsync_ShouldRename()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync();

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, name: "Renamed"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Name.ShouldBe("Renamed");
    }

    [Fact(DisplayName = "HandleAsync should clear the tournament cache tag once after a successful save")]
    public async Task HandleAsync_ShouldEvictTournamentCache_AfterSave()
    {
        // Arrange
        var series = await SeedAsync();
        var cache = SeriesCache.Evicting();

        // Act
        var result = await CreateHandler(cache: cache).HandleAsync(Command(series, name: "Renamed"), TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        SeriesCache.VerifyEvictedOnce(cache);
    }

    [Fact(DisplayName = "HandleAsync should not touch the tournament cache when the edit fails")]
    public async Task HandleAsync_ShouldNotEvictTournamentCache_WhenEditFails()
    {
        // Arrange
        var series = await SeedAsync(status: StageStatus.Computed);
        var changed = Current(series).Skip(1).ToList();

        // Act (an untouched strict cache fails the test if it is called)
        var result = await CreateHandler(cache: SeriesCache.Untouched()).HandleAsync(Command(series, changed), TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should not report the Series' own tournaments as held by another Series")]
    public async Task HandleAsync_ShouldNotConflictWithOwnTournaments()
    {
        // Arrange
        var series = await SeedAsync();

        // Act
        var result = await CreateHandler().HandleAsync(Command(series), TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should remove the first Stage and shift the rest up")]
    public async Task HandleAsync_ShouldRemoveFirstStage_AndShiftOrders()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync();
        var remaining = Current(series).Skip(1).ToList();

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, remaining), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _dbContext.ChangeTracker.Clear();
        var saved = await _dbContext.Series.AsNoTracking().Include(s => s.Stages).SingleAsync(ct);
        saved.Stages.Select(s => s.Order).OrderBy(o => o).ShouldBe([1, 2]);
        saved.Stages.Select(s => s.Kind).ShouldBe([StageKind.Survivor, StageKind.Placement], ignoreOrder: true);
    }

    [Fact(DisplayName = "HandleAsync should add a Stage in the middle and shift the later ones down")]
    public async Task HandleAsync_ShouldInsertStageInTheMiddle()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync();
        var season = await _dbContext.Seasons.AsNoTracking().SingleAsync(ct);
        var extra = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Extra", new DateOnly(2025, 2, 15), null, ct);
        var stages = Current(series);
        stages.Insert(1, SeriesStageDefinitionFactory.Create(tournamentId: extra.Id));

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, stages), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _dbContext.ChangeTracker.Clear();
        var saved = await _dbContext.Series.AsNoTracking().Include(s => s.Stages).SingleAsync(ct);
        saved.Stages.OrderBy(s => s.Order).Select(s => s.Kind).ShouldBe(
            [StageKind.Qualifying, StageKind.Qualifying, StageKind.Survivor, StageKind.Placement]);
    }

    [Fact(DisplayName = "HandleAsync should give a new Stage the tournament of the Stage it replaces, despite the unique tournament index")]
    public async Task HandleAsync_ShouldReuseRemovedStageTournament_ForNewStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync();
        var original = Current(series);
        var replacement = SeriesStageDefinitionFactory.Create(
            kind: original[0].Kind, tournamentId: original[0].TournamentId, advancingCount: original[0].AdvancingCount);
        var stages = new List<SeriesStageDefinition> { replacement, original[1], original[2] };

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, stages), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _dbContext.ChangeTracker.Clear();
        var saved = await _dbContext.Series.AsNoTracking().Include(s => s.Stages).SingleAsync(ct);
        saved.Stages.Count.ShouldBe(3);
        saved.Stages.Select(s => s.Id).ShouldNotContain(series.Stages.First().Id);
        saved.Stages.Single(s => s.Order == 1).TournamentId.ShouldBe(original[0].TournamentId);
    }

    [Fact(DisplayName = "HandleAsync should move a removed Stage's tournament and order to a kept Stage, despite the unique indexes")]
    public async Task HandleAsync_ShouldMoveRemovedStageSlot_ToKeptStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync();
        var original = Current(series);

        // The Survivor Stage goes; the Placement Stage keeps its id and takes the Survivor's tournament and order.
        var placement = original[2] with { TournamentId = original[1].TournamentId };
        var stages = new List<SeriesStageDefinition> { original[0], placement };

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, stages), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _dbContext.ChangeTracker.Clear();
        var saved = await _dbContext.Series.AsNoTracking().Include(s => s.Stages).SingleAsync(ct);
        saved.Stages.OrderBy(s => s.Order).Select(s => s.Order).ShouldBe([1, 2]);
        var kept = saved.Stages.Single(s => s.Id == placement.StageId);
        kept.Order.ShouldBe(2);
        kept.TournamentId.ShouldBe(original[1].TournamentId);
    }

    [Fact(DisplayName = "HandleAsync should return the domain errors and save nothing")]
    public async Task HandleAsync_ShouldReturnDomainErrors()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync();
        var broken = Current(series);
        broken[0] = broken[0] with { AdvancingCount = null };

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, broken, name: "Changed"), ct);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Stage.AdvancingCountRequired");
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Name.ShouldBe("VH");
    }

    [Fact(DisplayName = "HandleAsync should rename a locked Series with unchanged Stages")]
    public async Task HandleAsync_ShouldRenameLockedSeries()
    {
        // Arrange
        var series = await SeedAsync(status: StageStatus.Computed);

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, name: "Renamed"), TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should return Locked when a locked Series' Stages change")]
    public async Task HandleAsync_ShouldReturnLocked_WhenStagesChange()
    {
        // Arrange
        var series = await SeedAsync(status: StageStatus.Computed);
        var changed = Current(series).Skip(1).ToList();

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, changed), TestContext.Current.CancellationToken);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
    }

    [Fact(DisplayName = "HandleAsync should reschedule the Finale of a locked Series")]
    public async Task HandleAsync_ShouldRescheduleFinale_WhenLocked()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1)), status: StageStatus.Computed);
        var moved = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 8), startTime: new TimeOnly(18, 0));

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, finale: moved), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Finale!.Date.ShouldBe(new DateOnly(2025, 12, 8));
    }

    [Fact(DisplayName = "HandleAsync should return Locked when a locked Series' Finale is removed")]
    public async Task HandleAsync_ShouldReturnLocked_WhenFinaleIsRemovedWhileLocked()
    {
        // Arrange
        var series = await SeedAsync(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1)), status: StageStatus.Computed);

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, finale: null), TestContext.Current.CancellationToken);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Locked");
    }

    [Fact(DisplayName = "HandleAsync should return the center error and save nothing for an unknown Finale center")]
    public async Task HandleAsync_ShouldReturnCenterError_ForUnknownCenter()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var series = await SeedAsync(SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1)));
        var finale = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1), bowlingCenterId: CertificationNumberFactory.Create("99999"));

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, finale: finale), ct);

        // Assert
        result.Errors.Single().Code.ShouldBe("Series.Finale.BowlingCenter.NotAvailable");
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Finale!.BowlingCenterId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should keep a Finale center that has since closed")]
    public async Task HandleAsync_ShouldKeepClosedCenter()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var center = BowlingCenterFactory.Create(status: BowlingCenterStatus.Closed);
        _dbContext.BowlingCenters.Add(center);
        await _dbContext.SaveChangesAsync(ct);
        var finale = SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1), bowlingCenterId: center.CertificationNumber);
        var series = await SeedAsync(finale);

        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await CreateHandler().HandleAsync(Command(series, finale: finale, name: "Still fine"), ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should return LogoChangeDenied when the logo changes and the caller is denied EditSeriesLogo")]
    public async Task HandleAsync_ShouldReturnLogoChangeDenied_WhenDenied()
    {
        // Arrange
        var series = await SeedAsync();
        var command = Command(series, logo: StoredFileFactory.Create(container: "series", path: "new.png"));

        // Act
        var result = await CreateHandler(Denial(denied: true)).HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Logo.ChangeDenied");
        result.FirstError.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact(DisplayName = "HandleAsync should not read the denial when the logo is unchanged")]
    public async Task HandleAsync_ShouldSkipDenialRead_WhenLogoIsUnchanged()
    {
        // Arrange
        var logo = StoredFileFactory.Create(container: "series", path: "same.png");
        var series = await SeedAsync(logo: logo);

        // Act (a strict reader with no setup throws if it is called)
        var result = await CreateHandler(Denial(denied: true, expectLookup: false)).HandleAsync(Command(series, logo: logo), TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should return LogoNotUploaded when a new logo was not staged")]
    public async Task HandleAsync_ShouldReturnLogoNotUploaded()
    {
        // Arrange
        var series = await SeedAsync();
        var command = Command(series, logo: StoredFileFactory.Create(container: "series", path: "new.png"));

        // Act
        var result = await CreateHandler(Denial(denied: false)).HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Logo.NotUploaded");
    }

    [Fact(DisplayName = "HandleAsync should change the logo of a locked Series and schedule deletion of the old file")]
    public async Task HandleAsync_ShouldReplaceLogo_AndScheduleDeletion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var old = StoredFileFactory.Create(container: "series", path: "old.png");
        var series = await SeedAsync(logo: old, status: StageStatus.Computed);
        _dbContext.PendingUploads.Add(PendingUploadFactory.Create(container: "series", path: "new.png"));
        await _dbContext.SaveChangesAsync(ct);

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.Is<DeleteStoredFilesJob>(job => job.Files.Single().Path == "old.png"))).Returns("job");

        // Act
        var result = await CreateHandler(Denial(denied: false), scheduler.Object).HandleAsync(
            Command(series, logo: StoredFileFactory.Create(container: "series", path: "new.png")), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        scheduler.VerifyAll();
        _dbContext.ChangeTracker.Clear();
        (await _dbContext.Series.AsNoTracking().SingleAsync(ct)).Logo!.Path.ShouldBe("new.png");
        (await _dbContext.PendingUploads.CountAsync(ct)).ShouldBe(0);
    }
}