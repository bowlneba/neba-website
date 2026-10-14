using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.Api.Features.Tournaments.EditTournamentLogo;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;
using Neba.TestFactory.Uploads;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.Tournaments.EditTournamentLogo;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class EditTournamentLogoCommandHandlerTests(AppDbContextFixture fixture)
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

    private EditTournamentLogoCommandHandler CreateHandler(IBackgroundJobScheduler? backgroundJobScheduler = null)
    {
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();
        var scheduler = backgroundJobScheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object;
        return new EditTournamentLogoCommandHandler(_dbContext, cache, scheduler);
    }

    private async Task<(Season Season, Tournament Tournament)> SeedTournamentAsync(StoredFile? logo, CancellationToken ct)
    {
        var season = SeasonFactory.Create(startDate: new DateOnly(2025, 1, 1), endDate: new DateOnly(2025, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(
            startDate: season.StartDate,
            endDate: season.StartDate,
            seasonId: season.Id,
            logo: logo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        return (season, tournament);
    }

    [Fact(DisplayName = "HandleAsync returns Tournament.NotFound when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var command = new EditTournamentLogoCommand { TournamentId = TournamentId.New(), Logo = null };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync replaces the logo and enqueues deletion of the previous file")]
    public async Task HandleAsync_ShouldReplaceLogoAndEnqueueDeleteJob_WhenLogoIsReplaced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var previousLogo = StoredFileFactory.Create(container: "tournament-logos", path: "old-logo.png");
        var (_, tournament) = await SeedTournamentAsync(previousLogo, ct);
        var newLogo = StoredFileFactory.Create(container: "tournament-logos", path: "new-logo.png");
        await _dbContext.PendingUploads.AddAsync(PendingUploadFactory.Create(container: newLogo.Container, path: newLogo.Path), ct);
        await _dbContext.SaveChangesAsync(ct);

        DeleteTournamentFilesJob? scheduledJob = null;
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        schedulerMock
            .Setup(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()))
            .Callback<IBackgroundJob>(job => scheduledJob = (DeleteTournamentFilesJob)job)
            .Returns("job-id");

        var handler = CreateHandler(schedulerMock.Object);
        var command = new EditTournamentLogoCommand { TournamentId = tournament.Id, Logo = newLogo };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();

        var persisted = await _dbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.Logo.ShouldNotBeNull();
        persisted.Logo.Path.ShouldBe(newLogo.Path);

        scheduledJob.ShouldNotBeNull();
        var file = scheduledJob.Files.ShouldHaveSingleItem();
        file.Container.ShouldBe(previousLogo.Container);
        file.Path.ShouldBe(previousLogo.Path);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a delete job when the submitted logo equals the current logo")]
    public async Task HandleAsync_ShouldNotEnqueueDeleteJob_WhenLogoIsUnchanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "tournament-logos", path: "same-logo.png");
        var (_, tournament) = await SeedTournamentAsync(logo, ct);

        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(schedulerMock.Object);
        var command = new EditTournamentLogoCommand
        {
            TournamentId = tournament.Id,
            Logo = StoredFileFactory.Create(container: logo.Container, path: logo.Path, contentType: logo.ContentType, sizeInBytes: logo.SizeInBytes)
        };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        schedulerMock.Verify(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a delete job when only the logo metadata differs from the current logo")]
    public async Task HandleAsync_ShouldNotEnqueueDeleteJob_WhenOnlyMetadataDiffers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "tournament-logos", path: "same-logo.png", contentType: "image/png", sizeInBytes: 2048);
        var (_, tournament) = await SeedTournamentAsync(logo, ct);

        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(schedulerMock.Object);
        var command = new EditTournamentLogoCommand
        {
            TournamentId = tournament.Id,
            Logo = StoredFileFactory.Create(container: logo.Container, path: logo.Path, contentType: "image/jpeg", sizeInBytes: 1)
        };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await _dbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.Logo.ShouldNotBeNull();
        persisted.Logo.ContentType.ShouldBe("image/png");
        persisted.Logo.SizeInBytes.ShouldBe(2048);
    }

    [Fact(DisplayName = "HandleAsync clears the logo and enqueues deletion of the previous file when the logo is null")]
    public async Task HandleAsync_ShouldClearLogoAndEnqueueDeleteJob_WhenLogoIsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var previousLogo = StoredFileFactory.Create(container: "tournament-logos", path: "old-logo.png");
        var (_, tournament) = await SeedTournamentAsync(previousLogo, ct);

        DeleteTournamentFilesJob? scheduledJob = null;
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        schedulerMock
            .Setup(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()))
            .Callback<IBackgroundJob>(job => scheduledJob = (DeleteTournamentFilesJob)job)
            .Returns("job-id");

        var handler = CreateHandler(schedulerMock.Object);
        var command = new EditTournamentLogoCommand { TournamentId = tournament.Id, Logo = null };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();

        var persisted = await _dbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.Logo.ShouldBeNull();

        scheduledJob.ShouldNotBeNull();
        scheduledJob.Files.ShouldHaveSingleItem().Path.ShouldBe(previousLogo.Path);
    }

    [Fact(DisplayName = "HandleAsync does not enqueue a delete job when clearing a tournament that has no logo")]
    public async Task HandleAsync_ShouldNotEnqueueDeleteJob_WhenClearingTournamentWithoutLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (_, tournament) = await SeedTournamentAsync(logo: null, ct);

        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(schedulerMock.Object);
        var command = new EditTournamentLogoCommand { TournamentId = tournament.Id, Logo = null };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        schedulerMock.Verify(s => s.Enqueue(It.IsAny<DeleteTournamentFilesJob>()), Times.Never);
    }

    [Fact(DisplayName = "HandleAsync removes the pending upload record for the new logo")]
    public async Task HandleAsync_ShouldRemovePendingUpload_ForNewLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (_, tournament) = await SeedTournamentAsync(logo: null, ct);

        var logo = StoredFileFactory.Create(container: "logo-pending-container", path: "logo-pending.jpg");
        await _dbContext.PendingUploads.AddAsync(PendingUploadFactory.Create(container: logo.Container, path: logo.Path), ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new EditTournamentLogoCommand { TournamentId = tournament.Id, Logo = logo };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var stillPending = await _dbContext.PendingUploads.AsNoTracking()
            .AnyAsync(p => p.Container == logo.Container && p.Path == logo.Path, ct);
        stillPending.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync evicts the tournament and season cache tags")]
    public async Task HandleAsync_ShouldEvictTournamentAndSeasonCacheTags()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (season, tournament) = await SeedTournamentAsync(logo: null, ct);
        var cache = _serviceProvider.GetRequiredService<IFusionCache>();

        var tournamentKey = $"test:logo:tournament:{tournament.Id}";
        var seasonKey = $"test:logo:season:{season.Id}";
        await cache.GetOrSetAsync(tournamentKey, _ => Task.FromResult("cached-tournament"), tags: [$"neba:tournaments:{tournament.Id}"], token: ct);
        await cache.GetOrSetAsync(seasonKey, _ => Task.FromResult("cached-season"), tags: [$"neba:tournaments:{season.Id}"], token: ct);

        var newLogo = StoredFileFactory.Create(container: "tournament-logos", path: "new-logo.png");
        await _dbContext.PendingUploads.AddAsync(PendingUploadFactory.Create(container: newLogo.Container, path: newLogo.Path), ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = CreateHandler();
        var command = new EditTournamentLogoCommand { TournamentId = tournament.Id, Logo = newLogo };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var tournamentAfter = await cache.GetOrSetAsync(tournamentKey, _ => Task.FromResult("fresh-tournament"), token: ct);
        var seasonAfter = await cache.GetOrSetAsync(seasonKey, _ => Task.FromResult("fresh-season"), token: ct);
        tournamentAfter.ShouldBe("fresh-tournament");
        seasonAfter.ShouldBe("fresh-season");
    }

    [Fact(DisplayName = "HandleAsync rejects a logo that is not a pending upload or the current logo")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenLogoWasNeverUploaded()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var previousLogo = StoredFileFactory.Create(container: "tournament-logos", path: "old-logo.png");
        var (_, tournament) = await SeedTournamentAsync(previousLogo, ct);
        var foreignLogo = StoredFileFactory.Create(container: "documents", path: "bylaws.pdf");

        // Strict mock with no setups: the handler must not enqueue anything.
        var schedulerMock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        var handler = CreateHandler(schedulerMock.Object);
        var command = new EditTournamentLogoCommand { TournamentId = tournament.Id, Logo = foreignLogo };

        // Act
        var result = await handler.HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.Logo.NotUploaded");
        var persisted = await _dbContext.Tournaments.AsNoTracking().SingleAsync(t => t.Id == tournament.Id, ct);
        persisted.Logo.ShouldNotBeNull();
        persisted.Logo.Path.ShouldBe(previousLogo.Path);
    }
}