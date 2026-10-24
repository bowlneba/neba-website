using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.HallOfFame.EditHallOfFamePhoto;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Uploads;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Features.HallOfFame.EditHallOfFamePhoto;

[IntegrationTest]
[Component("HallOfFame")]
[Collection<AppDbContextFixture>]
public sealed class EditHallOfFamePhotoCommandHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private const string ListCacheTag = "neba:hall-of-fame:inductions";

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

    private IFusionCache Cache => _serviceProvider.GetRequiredService<IFusionCache>();

    private EditHallOfFamePhotoCommandHandler CreateHandler(IBackgroundJobScheduler? scheduler = null)
        => new(_dbContext, Cache, scheduler ?? new Mock<IBackgroundJobScheduler>(MockBehavior.Strict).Object);

    private async Task<HallOfFameInduction> SeedInductionAsync(
        StoredFile? photo,
        CancellationToken ct,
        int? focalX = null,
        int? focalY = null,
        int? zoom = null)
    {
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);

        var induction = HallOfFameInductionFactory.Create(
            bowlerId: bowler.Id,
            photo: photo,
            photoFocalX: focalX,
            photoFocalY: focalY,
            photoZoom: zoom);
        await _dbContext.HallOfFameInductions.AddAsync(induction, ct);
        await _dbContext.SaveChangesAsync(ct);

        return induction;
    }

    private async Task AddPendingUploadAsync(StoredFile file, CancellationToken ct)
    {
        await _dbContext.PendingUploads.AddAsync(PendingUploadFactory.Create(container: file.Container, path: file.Path), ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<HallOfFameInduction> ReloadAsync(HallOfFameId id, CancellationToken ct)
    {
        await using var readContext = fixture.CreateDbContext();
        return await readContext.HallOfFameInductions.AsNoTracking().SingleAsync(i => i.Id == id, ct);
    }

    private static Mock<IBackgroundJobScheduler> ExpectDeleteJob(Action<DeleteStoredFilesJob> capture)
    {
        var mock = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        mock.Setup(s => s.Enqueue(It.IsAny<DeleteStoredFilesJob>()))
            .Callback<IBackgroundJob>(job => capture((DeleteStoredFilesJob)job))
            .Returns("job-id");
        return mock;
    }

    [Fact(DisplayName = "HandleAsync returns HallOfFame.NotFound when the induction does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenInductionDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var command = EditHallOfFamePhotoCommandFactory.Create();

        // Act
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("HallOfFame.NotFound");
    }

    [Fact(DisplayName = "HandleAsync sets a new photo from a pending upload with the centered default framing")]
    public async Task HandleAsync_ShouldSetPhotoWithDefaultFraming_WhenNoFramingIsSent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var induction = await SeedInductionAsync(photo: null, ct);
        var photo = StoredFileFactory.Create(container: "bowlneba-public", path: "hall-of-fame/photo/new.png");
        await AddPendingUploadAsync(photo, ct);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, photo);

        // Act
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await ReloadAsync(induction.Id, ct);
        persisted.Photo.ShouldNotBeNull().Path.ShouldBe(photo.Path);
        persisted.PhotoFocalPoint.ShouldBe(PhotoFocalPoint.Default);
        (await _dbContext.PendingUploads.AnyAsync(p => p.Path == photo.Path, ct)).ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync sets a new photo with the framing that was sent")]
    public async Task HandleAsync_ShouldSetPhotoWithFraming_WhenFramingIsSent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var induction = await SeedInductionAsync(photo: null, ct);
        var photo = StoredFileFactory.Create(container: "bowlneba-public", path: "hall-of-fame/photo/new.png");
        await AddPendingUploadAsync(photo, ct);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, photo, focalX: 31, focalY: 36, zoom: 175);

        // Act
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await ReloadAsync(induction.Id, ct);
        persisted.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(31, 36, 175));
    }

    [Fact(DisplayName = "HandleAsync evicts the Hall of Fame list from the cache")]
    public async Task HandleAsync_ShouldEvictListCache_WhenPhotoChanges()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var induction = await SeedInductionAsync(photo: null, ct);
        var photo = StoredFileFactory.Create(path: "hall-of-fame/photo/new.png");
        await AddPendingUploadAsync(photo, ct);
        await Cache.SetAsync("neba:hall-of-fame:inductions:list", "cached", tags: [ListCacheTag], token: ct);

        // Act
        await CreateHandler().HandleAsync(EditHallOfFamePhotoCommandFactory.Create(induction.Id, photo), ct);

        // Assert
        var cached = await Cache.TryGetAsync<string>("neba:hall-of-fame:inductions:list", token: ct);
        cached.HasValue.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync replaces the photo and enqueues deletion of the previous file")]
    public async Task HandleAsync_ShouldReplacePhotoAndEnqueueDeleteJob_WhenPhotoIsReplaced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var previous = StoredFileFactory.Create(container: "bowlneba-public", path: "hall-of-fame/photo/old.png");
        var induction = await SeedInductionAsync(previous, ct, focalX: 10, focalY: 20, zoom: 200);
        var replacement = StoredFileFactory.Create(container: "bowlneba-public", path: "hall-of-fame/photo/new.png");
        await AddPendingUploadAsync(replacement, ct);

        DeleteStoredFilesJob? scheduled = null;
        var scheduler = ExpectDeleteJob(job => scheduled = job);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, replacement, focalX: 60, focalY: 70, zoom: 100);

        // Act
        var result = await CreateHandler(scheduler.Object).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await ReloadAsync(induction.Id, ct);
        persisted.Photo.ShouldNotBeNull().Path.ShouldBe(replacement.Path);
        persisted.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(60, 70, 100));

        var file = scheduled.ShouldNotBeNull().Files.ShouldHaveSingleItem();
        file.Container.ShouldBe(previous.Container);
        file.Path.ShouldBe(previous.Path);
    }

    [Fact(DisplayName = "HandleAsync returns PhotoNotUploaded and changes nothing when the photo is not a pending upload")]
    public async Task HandleAsync_ShouldReturnPhotoNotUploaded_WhenPhotoIsNotPending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var previous = StoredFileFactory.Create(path: "hall-of-fame/photo/old.png");
        var induction = await SeedInductionAsync(previous, ct, focalX: 10, focalY: 20, zoom: 200);
        var arbitrary = StoredFileFactory.Create(path: "somewhere/else.png");
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, arbitrary);

        // Act (a strict scheduler with no setup throws if a delete job is queued)
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe("HallOfFame.Photo.NotUploaded");
        var persisted = await ReloadAsync(induction.Id, ct);
        persisted.Photo.ShouldNotBeNull().Path.ShouldBe(previous.Path);
        persisted.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(10, 20, 200));
    }

    [Fact(DisplayName = "HandleAsync returns a validation error and changes nothing when the framing is out of range")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenFramingIsOutOfRange()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var induction = await SeedInductionAsync(photo: null, ct);
        var photo = StoredFileFactory.Create(path: "hall-of-fame/photo/new.png");
        await AddPendingUploadAsync(photo, ct);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, photo, focalX: 101, focalY: 50, zoom: 100);

        // Act
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("HallOfFame.Photo.FocalPointOutOfRange");
        (await ReloadAsync(induction.Id, ct)).Photo.ShouldBeNull();
        (await _dbContext.PendingUploads.AnyAsync(p => p.Path == photo.Path, ct)).ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync clears the photo and focal point and enqueues deletion of the file")]
    public async Task HandleAsync_ShouldClearPhotoAndEnqueueDeleteJob_WhenPhotoIsRemoved()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var previous = StoredFileFactory.Create(container: "bowlneba-public", path: "hall-of-fame/photo/old.png");
        var induction = await SeedInductionAsync(previous, ct, focalX: 10, focalY: 20, zoom: 200);

        DeleteStoredFilesJob? scheduled = null;
        var scheduler = ExpectDeleteJob(job => scheduled = job);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id);

        // Act
        var result = await CreateHandler(scheduler.Object).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await ReloadAsync(induction.Id, ct);
        persisted.Photo.ShouldBeNull();
        persisted.PhotoFocalPoint.ShouldBeNull();
        scheduled.ShouldNotBeNull().Files.ShouldHaveSingleItem().Path.ShouldBe(previous.Path);
    }

    [Fact(DisplayName = "HandleAsync succeeds without a delete job when the photo is already removed")]
    public async Task HandleAsync_ShouldSucceedWithoutDeleteJob_WhenPhotoIsAlreadyRemoved()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var induction = await SeedInductionAsync(photo: null, ct);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id);

        // Act (a strict scheduler with no setup throws if a delete job is queued)
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        (await ReloadAsync(induction.Id, ct)).Photo.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync changes only the framing when the same photo is sent again, keeping the stored metadata")]
    public async Task HandleAsync_ShouldChangeOnlyFraming_WhenSamePhotoIsSentAgain()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var stored = StoredFileFactory.Create(path: "hall-of-fame/photo/same.png", contentType: "image/png", sizeInBytes: 100);
        var induction = await SeedInductionAsync(stored, ct, focalX: 10, focalY: 20, zoom: 200);
        var resent = StoredFileFactory.Create(path: stored.Path, contentType: "image/jpeg", sizeInBytes: 999_999);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, resent, focalX: 80, focalY: 90, zoom: 120);

        // Act (a strict scheduler with no setup throws if a delete job is queued)
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await ReloadAsync(induction.Id, ct);
        persisted.Photo.ShouldNotBeNull().ContentType.ShouldBe("image/png");
        persisted.Photo.SizeInBytes.ShouldBe(100);
        persisted.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(80, 90, 120));
    }

    [Fact(DisplayName = "HandleAsync keeps the current framing when the same photo is sent with no framing")]
    public async Task HandleAsync_ShouldKeepFraming_WhenSamePhotoIsSentWithNoFraming()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var stored = StoredFileFactory.Create(path: "hall-of-fame/photo/same.png");
        var induction = await SeedInductionAsync(stored, ct, focalX: 10, focalY: 20, zoom: 200);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, stored);

        // Act
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        (await ReloadAsync(induction.Id, ct)).PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(10, 20, 200));
    }

    [Fact(DisplayName = "HandleAsync keeps the photo and changes the framing when only the framing is sent")]
    public async Task HandleAsync_ShouldKeepPhotoAndChangeFraming_WhenOnlyFramingIsSent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var stored = StoredFileFactory.Create(path: "hall-of-fame/photo/same.png");
        var induction = await SeedInductionAsync(stored, ct, focalX: 10, focalY: 20, zoom: 200);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, photo: null, focalX: 5, focalY: 95, zoom: 300);

        // Act (a strict scheduler with no setup throws if a delete job is queued)
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var persisted = await ReloadAsync(induction.Id, ct);
        persisted.Photo.ShouldNotBeNull().Path.ShouldBe(stored.Path);
        persisted.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(5, 95, 300));
    }

    [Fact(DisplayName = "HandleAsync returns a conflict when only the framing is sent and the induction has no photo")]
    public async Task HandleAsync_ShouldReturnConflict_WhenOnlyFramingIsSentAndNoPhoto()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var induction = await SeedInductionAsync(photo: null, ct);
        var command = EditHallOfFamePhotoCommandFactory.Create(induction.Id, photo: null, focalX: 5, focalY: 95, zoom: 150);

        // Act
        var result = await CreateHandler().HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("HallOfFame.Photo.Missing");
    }
}