using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.CreateTournament;
using Neba.Api.Features.Tournaments.Domain;
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

namespace Neba.Api.Tests.Features.Tournaments.CreateTournament;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class CreateTournamentCommandHandlerTests(AppDbContextFixture fixture)
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

    private CreateTournamentCommandHandler CreateHandler(
        IBackgroundJobScheduler? backgroundJobScheduler = null,
        TimeProvider? timeProvider = null)
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var scheduler = backgroundJobScheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object;
        return new CreateTournamentCommandHandler(_dbContext, cache, scheduler, timeProvider ?? new FakeTimeProvider(Now));
    }

    private async Task<Season> SeedSeasonAsync(CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);
        return season;
    }

    private static CreateTournamentCommand ValidCommand(
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

    [Fact(DisplayName = "HandleAsync returns validation error when no season contains the tournament dates")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenNoSeasonForDates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(startDate: new DateOnly(2099, 1, 1), endDate: new DateOnly(2099, 1, 2));

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NoSeasonForDates");
    }

    [Fact(DisplayName = "HandleAsync does not persist a tournament when no season contains the tournament dates")]
    public async Task HandleAsync_ShouldNotPersistTournament_WhenNoSeasonForDates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = ValidCommand(startDate: new DateOnly(2099, 1, 1), endDate: new DateOnly(2099, 1, 2));

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var count = await _dbContext.Tournaments.CountAsync(ct);
        count.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the bowling center is not found")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenBowlingCenterNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var missingCertificationNumber = CertificationNumberFactory.Create("99999");
        var command = ValidCommand(bowlingCenterId: missingCertificationNumber);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.BowlingCenterNotFound");
    }

    [Fact(DisplayName = "HandleAsync succeeds when the bowling center exists")]
    public async Task HandleAsync_ShouldSucceed_WhenBowlingCenterExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var bowlingCenter = BowlingCenterFactory.Create();
        await _dbContext.BowlingCenters.AddAsync(bowlingCenter, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(bowlingCenterId: bowlingCenter.CertificationNumber);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the oil pattern is not found")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenOilPatternNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(oilPatternId: OilPatternId.New());

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.OilPatternNotFound");
    }

    [Fact(DisplayName = "HandleAsync derives the pattern length and ratio categories from the oil pattern when provided")]
    public async Task HandleAsync_ShouldDerivePatternCategoriesFromOilPattern_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var oilPattern = OilPatternFactory.Create(length: 45, leftRatio: 2m, rightRatio: 2m);
        await _dbContext.OilPatterns.AddAsync(oilPattern, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(
            name: "Tournament With Oil Pattern",
            oilPatternId: oilPattern.Id,
            // Values below should be ignored/overridden since the oil pattern is provided
            patternLengthCategory: PatternLengthCategory.ShortPattern,
            patternRatioCategory: PatternRatioCategory.Recreation);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Name == "Tournament With Oil Pattern", ct);
        persisted.PatternLengthCategory.ShouldBe(oilPattern.LengthCategory);
        persisted.PatternRatioCategory.ShouldBe(oilPattern.RatioCategory);
    }

    [Fact(DisplayName = "HandleAsync attaches the oil pattern to the tournament when provided")]
    public async Task HandleAsync_ShouldAttachOilPatternToTournament_WhenProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var oilPattern = OilPatternFactory.Create();
        await _dbContext.OilPatterns.AddAsync(oilPattern, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: "Tournament With Attached Oil Pattern", oilPatternId: oilPattern.Id);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Name == "Tournament With Attached Oil Pattern", ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var getTournamentHandler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);
        var getResult = await getTournamentHandler.HandleAsync(
            new GetTournamentQuery { Id = persisted.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true },
            ct);

        getResult.IsError.ShouldBeFalse();
        var attachedPattern = getResult.Value.OilPatterns.ShouldHaveSingleItem();
        attachedPattern.TournamentRounds.ShouldBe(["Qualifying", "Match Play"]);
    }

    [Fact(DisplayName = "HandleAsync does not attach an oil pattern to the tournament when none is specified")]
    public async Task HandleAsync_ShouldNotAttachOilPattern_WhenNoneSpecified()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(name: "Tournament Without Attached Oil Pattern");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Name == "Tournament Without Attached Oil Pattern", ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var getTournamentHandler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);
        var getResult = await getTournamentHandler.HandleAsync(
            new GetTournamentQuery { Id = persisted.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true },
            ct);

        getResult.IsError.ShouldBeFalse();
        getResult.Value.OilPatterns.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync uses the provided pattern categories when no oil pattern is specified")]
    public async Task HandleAsync_ShouldUseProvidedPatternCategories_WhenNoOilPatternSpecified()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(
            name: "Tournament Without Oil Pattern",
            patternLengthCategory: PatternLengthCategory.LongPattern,
            patternRatioCategory: PatternRatioCategory.Sport);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Name == "Tournament Without Oil Pattern", ct);
        persisted.PatternLengthCategory.ShouldBe(PatternLengthCategory.LongPattern);
        persisted.PatternRatioCategory.ShouldBe(PatternRatioCategory.Sport);
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when tournament creation fails")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenTournamentCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(name: string.Empty);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Name.Required");
    }

    [Fact(DisplayName = "HandleAsync does not persist a tournament when tournament creation fails")]
    public async Task HandleAsync_ShouldNotPersistTournament_WhenTournamentCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(name: string.Empty);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var count = await _dbContext.Tournaments.CountAsync(ct);
        count.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync persists the tournament when command is valid")]
    public async Task HandleAsync_ShouldPersistTournament_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(name: "My New Tournament", statsEligible: false, entryFee: 50m);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Name == "My New Tournament", ct);
        persisted.EntryFee.ShouldBe(50m);
        persisted.StatsEligible.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync persists the NEBA added money when command is valid")]
    public async Task HandleAsync_ShouldPersistNebaAddedMoney_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(name: "Tournament With NEBA Added Money", nebaAddedMoney: 750m);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Name == "Tournament With NEBA Added Money", ct);
        persisted.NebaAddedMoney.ShouldBe(750m);
    }

    [Fact(DisplayName = "HandleAsync returns the created tournament's id when command is valid")]
    public async Task HandleAsync_ShouldReturnCreatedTournamentId_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(name: "Tournament With Returned Id");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Name == "Tournament With Returned Id", ct);
        result.Value.ShouldBe(persisted.Id);
    }

    [Fact(DisplayName = "HandleAsync assigns the tournament to the matched season")]
    public async Task HandleAsync_ShouldAssignTournamentToMatchedSeason_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(ct);
        var handler = CreateHandler();
        var command = ValidCommand(name: "Tournament With Season");

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking()
            .SingleAsync(t => t.Name == "Tournament With Season", ct);
        persisted.SeasonId.ShouldBe(season.Id);
    }

    [Fact(DisplayName = "HandleAsync invalidates the season's tournaments cache tag when command is valid")]
    public async Task HandleAsync_ShouldInvalidateTournamentsCacheTag_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var listCacheKey = $"neba:tournaments:{season.Id}:list";
        var cacheTag = $"neba:tournaments:{season.Id}";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: "Cache Invalidation Tournament");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var listAfterCreate = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterCreate.ShouldBe("fresh-list");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the cache tag when tournament creation fails")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenTournamentCreationFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var listCacheKey = $"neba:tournaments:{season.Id}:list";
        var cacheTag = $"neba:tournaments:{season.Id}";

        await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("cached-list"),
            tags: [cacheTag],
            token: ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: string.Empty);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was created
        var listAfterCreate = await cache.GetOrSetAsync(
            listCacheKey,
            _ => Task.FromResult("fresh-list"),
            token: ct);
        listAfterCreate.ShouldBe("cached-list");
    }

    [Fact(DisplayName = "HandleAsync removes the pending upload record for the logo when command is valid")]
    public async Task HandleAsync_ShouldRemovePendingUpload_ForLogo_WhenCommandIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var logo = StoredFileFactory.Create(container: "logo-pending-container", path: "logo-pending.jpg");
        var pendingUpload = PendingUploadFactory.Create(container: logo.Container, path: logo.Path);
        await _dbContext.PendingUploads.AddAsync(pendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: "Tournament With Logo Pending Upload", logo: logo);

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == logo.Container && p.Path == logo.Path, ct);
        stillPending.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync does not remove unrelated pending upload records")]
    public async Task HandleAsync_ShouldNotRemoveUnrelatedPendingUploads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var unrelatedPendingUpload = PendingUploadFactory.Create(container: "unrelated-container", path: "unrelated-file.jpg");
        await _dbContext.PendingUploads.AddAsync(unrelatedPendingUpload, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = ValidCommand(name: "Tournament Without Matching Pending Upload");

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == "unrelated-container" && p.Path == "unrelated-file.jpg", ct);
        stillPending.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync schedules an oil pattern reveal cache eviction job when a future reveal date is provided")]
    public async Task HandleAsync_ShouldScheduleEvictionJob_WhenOilPatternRevealDateTimeIsInFuture()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var timeProvider = new FakeTimeProvider(Now);
        var revealAt = Now.AddDays(3);
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        schedulerMock
            .Setup(scheduler => scheduler.Schedule(It.IsAny<EvictOilPatternRevealCacheJob>(), revealAt))
            .Returns("job-id");

        var handler = CreateHandler(schedulerMock.Object, timeProvider);
        var command = ValidCommand(name: "Tournament With Future Reveal Date", oilPatternRevealDateTime: revealAt);

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
        await SeedSeasonAsync(ct);
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(schedulerMock.Object);
        var command = ValidCommand(name: "Tournament Without Reveal Date", oilPatternRevealDateTime: null);

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync does not schedule an eviction job when the reveal date is already in the past")]
    public async Task HandleAsync_ShouldNotScheduleEvictionJob_WhenOilPatternRevealDateTimeIsInPast()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedSeasonAsync(ct);
        var timeProvider = new FakeTimeProvider(Now);
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(schedulerMock.Object, timeProvider);
        var command = ValidCommand(name: "Tournament With Past Reveal Date", oilPatternRevealDateTime: Now.AddDays(-1));

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }
}