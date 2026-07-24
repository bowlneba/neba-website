using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;
using Neba.Api.Features.Tournaments.GetTournament;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;
using Neba.TestFactory.Uploads;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.EditTournament;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class EditTournamentCommandHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private ServiceProvider _serviceProvider = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        var services = new ServiceCollection();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromHours(1));
        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private EditTournamentCommandHandler CreateHandler(
        IBackgroundJobScheduler? backgroundJobScheduler = null,
        TimeProvider? timeProvider = null)
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var scheduler = backgroundJobScheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object;
        return new EditTournamentCommandHandler(_dbContext, cache, scheduler, timeProvider ?? new FakeTimeProvider(Now));
    }

    private async Task<Season> SeedSeasonAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var season = SeasonFactory.Create(startDate: startDate, endDate: endDate);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        return season;
    }

    private async Task<Tournament> SeedTournamentAsync(Season season, CancellationToken ct)
    {
        var tournament = TournamentFactory.Create(
            startDate: season.StartDate,
            endDate: season.StartDate,
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    private static EditTournamentCommand ValidCommand(
        TournamentId tournamentId,
        string? name = null,
        TournamentType? tournamentType = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? statsEligible = null,
        decimal? entryFee = null,
        decimal? nebaAddedMoney = null,
        CertificationNumber? bowlingCenterId = null,
        Uri? externalRegistrationUrl = null,
        StoredFile? logo = null,
        OilPatternId? oilPatternId = null,
        PatternLengthCategory? patternLengthCategory = null,
        PatternRatioCategory? patternRatioCategory = null,
        DateTimeOffset? oilPatternRevealDateTime = null)
        => new()
        {
            TournamentId = tournamentId,
            Name = name ?? TournamentFactory.ValidName,
            TournamentType = tournamentType ?? TournamentFactory.ValidTournamentType,
            StartDate = startDate ?? TournamentFactory.ValidStartDate,
            EndDate = endDate ?? TournamentFactory.ValidEndDate,
            StatsEligible = statsEligible ?? true,
            EntryFee = entryFee ?? 100m,
            NebaAddedMoney = nebaAddedMoney ?? 0m,
            BowlingCenterId = bowlingCenterId,
            ExternalRegistrationUrl = externalRegistrationUrl,
            Logo = logo,
            OilPatternId = oilPatternId,
            PatternLengthCategory = patternLengthCategory,
            PatternRatioCategory = patternRatioCategory,
            OilPatternRevealDateTime = oilPatternRevealDateTime
        };

    [Fact(DisplayName = "HandleAsync returns Tournament.NotFound when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(TournamentId.New());

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns validation error when no season contains the submitted dates")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenNoSeasonForDates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, startDate: new DateOnly(2099, 1, 1), endDate: new DateOnly(2099, 1, 2));

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NoSeasonForDates");
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the bowling center is not found")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenBowlingCenterNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var handler = CreateHandler();
        var missingCertificationNumber = CertificationNumberFactory.Create("99999");
        var command = ValidCommand(
            tournament.Id,
            startDate: season.StartDate,
            endDate: season.StartDate,
            bowlingCenterId: missingCertificationNumber);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.BowlingCenterNotFound");
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the oil pattern is not found")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenOilPatternNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, startDate: season.StartDate, endDate: season.StartDate, oilPatternId: OilPatternId.New());

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.OilPatternNotFound");
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the update fails")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenUpdateFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, name: string.Empty, startDate: season.StartDate, endDate: season.StartDate);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Name.Required");
    }

    [Fact(DisplayName = "HandleAsync persists updated fields when command is valid")]
    public async Task HandleAsync_ShouldPersistUpdatedFields_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var handler = CreateHandler();
        var command = ValidCommand(
            tournament.Id,
            name: "Updated Tournament Name",
            startDate: season.StartDate,
            endDate: season.StartDate,
            statsEligible: false,
            entryFee: 250m,
            nebaAddedMoney: 500m);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.Name.ShouldBe("Updated Tournament Name");
        persisted.StatsEligible.ShouldBeFalse();
        persisted.EntryFee.ShouldBe(250m);
        persisted.NebaAddedMoney.ShouldBe(500m);
    }

    [Fact(DisplayName = "HandleAsync re-derives the season when the submitted dates fall in a different season")]
    public async Task HandleAsync_ShouldRederiveSeason_WhenDatesFallInDifferentSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var originalSeason = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 6, 30), ct);
        var newSeason = await SeedSeasonAsync(new DateOnly(2025, 7, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(originalSeason, ct);
        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, startDate: newSeason.StartDate, endDate: newSeason.StartDate);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.SeasonId.ShouldBe(newSeason.Id);
    }

    [Fact(DisplayName = "HandleAsync invalidates the tournament and previous season cache tags when command is valid")]
    public async Task HandleAsync_ShouldInvalidateTournamentAndSeasonCacheTags_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        var tournamentCacheKey = $"neba:tournaments:{tournament.Id}:detail";
        var tournamentCacheTag = $"neba:tournaments:{tournament.Id}";
        var seasonCacheKey = $"neba:tournaments:{season.Id}:list";
        var seasonCacheTag = $"neba:tournaments:{season.Id}";

        await cache.GetOrSetAsync(tournamentCacheKey, _ => Task.FromResult("cached-detail"), tags: [tournamentCacheTag], token: ct);
        await cache.GetOrSetAsync(seasonCacheKey, _ => Task.FromResult("cached-list"), tags: [seasonCacheTag], token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, name: "Cache Invalidation Tournament", startDate: season.StartDate, endDate: season.StartDate);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var detailAfterEdit = await cache.GetOrSetAsync(tournamentCacheKey, _ => Task.FromResult("fresh-detail"), token: ct);
        detailAfterEdit.ShouldBe("fresh-detail");
        var listAfterEdit = await cache.GetOrSetAsync(seasonCacheKey, _ => Task.FromResult("fresh-list"), token: ct);
        listAfterEdit.ShouldBe("fresh-list");
    }

    [Fact(DisplayName = "HandleAsync invalidates the new season's cache tag when the season changes")]
    public async Task HandleAsync_ShouldInvalidateNewSeasonCacheTag_WhenSeasonChanges()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var originalSeason = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 6, 30), ct);
        var newSeason = await SeedSeasonAsync(new DateOnly(2025, 7, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(originalSeason, ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        var newSeasonCacheKey = $"neba:tournaments:{newSeason.Id}:list";
        var newSeasonCacheTag = $"neba:tournaments:{newSeason.Id}";
        await cache.GetOrSetAsync(newSeasonCacheKey, _ => Task.FromResult("cached-list"), tags: [newSeasonCacheTag], token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, startDate: newSeason.StartDate, endDate: newSeason.StartDate);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var listAfterEdit = await cache.GetOrSetAsync(newSeasonCacheKey, _ => Task.FromResult("fresh-list"), token: ct);
        listAfterEdit.ShouldBe("fresh-list");
    }

    [Fact(DisplayName = "HandleAsync enqueues a delete job for the previous logo when the logo is replaced")]
    public async Task HandleAsync_ShouldEnqueueDeleteJob_WhenLogoIsReplaced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var previousLogo = StoredFileFactory.Create(container: "tournament-logos", path: "old-logo.png");
        var tournament = TournamentFactory.Create(
            startDate: season.StartDate,
            endDate: season.StartDate,
            seasonId: season.Id,
            logo: previousLogo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var newLogo = StoredFileFactory.Create(container: "tournament-logos", path: "new-logo.png");

        DeleteTournamentFilesJob? scheduledJob = null;
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        schedulerMock
            .Setup(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()))
            .Callback<IBackgroundJob>(job => scheduledJob = (DeleteTournamentFilesJob)job)
            .Returns("job-id");

        var handler = CreateHandler(schedulerMock.Object);
        var command = ValidCommand(tournament.Id, startDate: season.StartDate, endDate: season.StartDate, logo: newLogo);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        scheduledJob.ShouldNotBeNull();
        var file = scheduledJob.Files.ShouldHaveSingleItem();
        file.Container.ShouldBe(previousLogo.Container);
        file.Path.ShouldBe(previousLogo.Path);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a delete job when the logo is unchanged")]
    public async Task HandleAsync_ShouldNotEnqueueDeleteJob_WhenLogoIsUnchanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var logo = StoredFileFactory.Create(container: "tournament-logos", path: "same-logo.png");
        var tournament = TournamentFactory.Create(
            startDate: season.StartDate,
            endDate: season.StartDate,
            seasonId: season.Id,
            logo: logo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(schedulerMock.Object);
        var command = ValidCommand(
            tournament.Id,
            startDate: season.StartDate,
            endDate: season.StartDate,
            logo: StoredFileFactory.Create(container: logo.Container, path: logo.Path, contentType: logo.ContentType, sizeInBytes: logo.SizeInBytes));

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync removes the pending upload record for the logo when command is valid")]
    public async Task HandleAsync_ShouldRemovePendingUpload_ForLogo_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);

        var logo = StoredFileFactory.Create(container: "logo-pending-container", path: "logo-pending.jpg");
        var pendingUpload = PendingUploadFactory.Create(container: logo.Container, path: logo.Path);
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, startDate: season.StartDate, endDate: season.StartDate, logo: logo);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == logo.Container && p.Path == logo.Path, ct);
        stillPending.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync attaches the oil pattern to the tournament when provided")]
    public async Task HandleAsync_ShouldAttachOilPatternToTournament_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var oilPattern = OilPatternFactory.Create();
        await _dbContext.OilPatterns.AddAsync(oilPattern, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(tournament.Id, startDate: season.StartDate, endDate: season.StartDate, oilPatternId: oilPattern.Id);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.PatternLengthCategory.ShouldBe(oilPattern.LengthCategory);
        persisted.PatternRatioCategory.ShouldBe(oilPattern.RatioCategory);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var getTournamentHandler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);
        var getResult = await getTournamentHandler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true },
            ct);

        getResult.IsError.ShouldBeFalse();
        var attachedPattern = getResult.Value.OilPatterns.ShouldHaveSingleItem();
        attachedPattern.TournamentRounds.ShouldBe(["Qualifying", "Match Play"]);
    }

    [Fact(DisplayName = "HandleAsync schedules an oil pattern reveal cache eviction job when a future reveal date is provided")]
    public async Task HandleAsync_ShouldScheduleEvictionJob_WhenOilPatternRevealDateTimeIsInFuture()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var timeProvider = new FakeTimeProvider(Now);
        var revealAt = Now.AddDays(3);
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        schedulerMock
            .Setup(scheduler => scheduler.Schedule(It.IsAny<EvictOilPatternRevealCacheJob>(), revealAt))
            .Returns("job-id");

        var handler = CreateHandler(schedulerMock.Object, timeProvider);
        var command = ValidCommand(
            tournament.Id, startDate: season.StartDate, endDate: season.StartDate, oilPatternRevealDateTime: revealAt);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync does not schedule an eviction job when no reveal date is provided")]
    public async Task HandleAsync_ShouldNotScheduleEvictionJob_WhenOilPatternRevealDateTimeIsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31), ct);
        var tournament = await SeedTournamentAsync(season, ct);
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(schedulerMock.Object);
        var command = ValidCommand(
            tournament.Id, startDate: season.StartDate, endDate: season.StartDate, oilPatternRevealDateTime: null);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }
}