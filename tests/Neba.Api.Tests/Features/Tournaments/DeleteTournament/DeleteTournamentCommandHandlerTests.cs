using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Database.Entities;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.DeleteTournament;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.DeleteTournament;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class DeleteTournamentCommandHandlerTests(AppDbContextFixture fixture)
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

    private DeleteTournamentCommandHandler CreateHandler(IBackgroundJobScheduler? backgroundJobScheduler = null)
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var scheduler = backgroundJobScheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object;
        return new DeleteTournamentCommandHandler(_dbContext, scheduler, cache);
    }

    private async Task<Tournament> SeedTournamentAsync(CancellationToken ct, StoredFile? logo = null)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id, logo: logo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);
        return tournament;
    }

    [Fact(DisplayName = "HandleAsync returns Deleted when tournament does not exist")]
    public async Task HandleAsync_ShouldReturnDeleted_WhenTournamentDoesNotExist()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = TournamentId.New() };

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync returns Deleted when tournament exists")]
    public async Task HandleAsync_ShouldReturnDeleted_WhenTournamentExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync removes the tournament from the database when it exists")]
    public async Task HandleAsync_ShouldRemoveTournamentFromDatabase_WhenTournamentExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillExists = await _dbContext.Tournaments.AsNoTracking()
            .AnyAsync(t => t.Id == tournament.Id, ct);
        stillExists.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync does not modify the database when tournament does not exist")]
    public async Task HandleAsync_ShouldNotModifyDatabase_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var existingTournament = await SeedTournamentAsync(ct);
        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = TournamentId.New() };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillExists = await _dbContext.Tournaments.AsNoTracking()
            .AnyAsync(t => t.Id == existingTournament.Id, ct);
        stillExists.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync returns a conflict error when the tournament has a historical champion")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenTournamentHasHistoricalChampion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.Set<HistoricalTournamentChampion>()
            .AddAsync(HistoricalTournamentChampionFactory.Create(bowler: bowler, tournament: tournament), ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.HasHistoricalRecords");
    }

    [Fact(DisplayName = "HandleAsync returns a conflict error when the tournament has a historical entry")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenTournamentHasHistoricalEntry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);

        await _dbContext.Set<HistoricalTournamentEntry>()
            .AddAsync(HistoricalTournamentEntryFactory.Create(tournament: tournament), ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.HasHistoricalRecords");
    }

    [Fact(DisplayName = "HandleAsync returns a conflict error when the tournament has a historical result")]
    public async Task HandleAsync_ShouldReturnConflictError_WhenTournamentHasHistoricalResult()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _dbContext.Set<HistoricalTournamentResult>()
            .AddAsync(HistoricalTournamentResultFactory.Create(bowler: bowler, tournament: tournament), ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.HasHistoricalRecords");
    }

    [Fact(DisplayName = "HandleAsync does not delete the tournament when it has historical records")]
    public async Task HandleAsync_ShouldNotDeleteTournament_WhenTournamentHasHistoricalRecords()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);

        await _dbContext.Set<HistoricalTournamentEntry>()
            .AddAsync(HistoricalTournamentEntryFactory.Create(tournament: tournament), ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        var stillExists = await _dbContext.Tournaments.AsNoTracking()
            .AnyAsync(t => t.Id == tournament.Id, ct);
        stillExists.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync enqueues a file deletion job for the tournament logo when it exists")]
    public async Task HandleAsync_ShouldEnqueueFileDeletionJob_WhenTournamentHasLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "logo-container", path: "logo.jpg");
        var tournament = await SeedTournamentAsync(ct, logo: logo);

        DeleteTournamentFilesJob? enqueuedJob = null;
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()))
            .Callback<DeleteTournamentFilesJob>(job => enqueuedJob = job)
            .Returns("job-id");
        var handler = CreateHandler(scheduler.Object);
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        enqueuedJob.ShouldNotBeNull();
        enqueuedJob.Files.ShouldContain(f => f.Container == logo.Container && f.Path == logo.Path);
        enqueuedJob.Files.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a file deletion job when tournament has no logo")]
    public async Task HandleAsync_ShouldNotEnqueueFileDeletionJob_WhenTournamentHasNoLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(scheduler.Object);
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a strict mock with no Enqueue setup would throw if called
        scheduler.Verify(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a file deletion job when tournament does not exist")]
    public async Task HandleAsync_ShouldNotEnqueueFileDeletionJob_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(scheduler.Object);
        var command = new DeleteTournamentCommand { TournamentId = TournamentId.New() };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        scheduler.Verify(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a file deletion job when tournament has historical records")]
    public async Task HandleAsync_ShouldNotEnqueueFileDeletionJob_WhenTournamentHasHistoricalRecords()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "logo-container", path: "logo.jpg");
        var tournament = await SeedTournamentAsync(ct, logo: logo);

        await _dbContext.Set<HistoricalTournamentEntry>()
            .AddAsync(HistoricalTournamentEntryFactory.Create(tournament: tournament), ct);
        await _dbContext.SaveChangesAsync(ct);

        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(scheduler.Object);
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert
        scheduler.Verify(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync invalidates the tournament and season cache tags when tournament exists")]
    public async Task HandleAsync_ShouldInvalidateTournamentAndSeasonCacheTags_WhenTournamentExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        var tournamentCacheKey = $"neba:tournaments:{tournament.Id}:detail";
        var seasonCacheKey = $"neba:tournaments:{tournament.SeasonId}:list";

        await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: [$"neba:tournaments:{tournament.Id}"],
            token: ct);
        await cache.GetOrSetAsync(
            seasonCacheKey,
            _ => Task.FromResult("cached-season-tournaments"),
            tags: [$"neba:tournaments:{tournament.SeasonId}"],
            token: ct);

        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — a stale cached value would be returned by GetOrSetAsync instead of invoking the factory
        var tournamentCacheAfterDelete = await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        tournamentCacheAfterDelete.ShouldBe("fresh-tournament");

        var seasonCacheAfterDelete = await cache.GetOrSetAsync(
            seasonCacheKey,
            _ => Task.FromResult("fresh-season-tournaments"),
            token: ct);
        seasonCacheAfterDelete.ShouldBe("fresh-season-tournaments");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the cache when tournament does not exist")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        const string tournamentCacheKey = "neba:tournaments:some-id:detail";

        await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: ["neba:tournaments:some-id"],
            token: ct);

        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = TournamentId.New() };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was deleted
        var tournamentCacheAfterDelete = await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        tournamentCacheAfterDelete.ShouldBe("cached-tournament");
    }

    [Fact(DisplayName = "HandleAsync does not invalidate the cache when tournament has historical records")]
    public async Task HandleAsync_ShouldNotInvalidateCache_WhenTournamentHasHistoricalRecords()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct);

        await _dbContext.Set<HistoricalTournamentEntry>()
            .AddAsync(HistoricalTournamentEntryFactory.Create(tournament: tournament), ct);
        await _dbContext.SaveChangesAsync(ct);

        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var tournamentCacheKey = $"neba:tournaments:{tournament.Id}:detail";

        await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("cached-tournament"),
            tags: [$"neba:tournaments:{tournament.Id}"],
            token: ct);

        var handler = CreateHandler();
        var command = new DeleteTournamentCommand { TournamentId = tournament.Id };

        // Act
        await handler.HandleAsync(command, ct);

        // Assert — the cached value survives since nothing was deleted
        var tournamentCacheAfterDelete = await cache.GetOrSetAsync(
            tournamentCacheKey,
            _ => Task.FromResult("fresh-tournament"),
            token: ct);
        tournamentCacheAfterDelete.ShouldBe("cached-tournament");
    }
}
