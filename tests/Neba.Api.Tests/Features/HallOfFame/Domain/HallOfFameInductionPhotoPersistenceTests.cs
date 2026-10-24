using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.HallOfFame.Domain;

[IntegrationTest]
[Component("HallOfFame")]
[Collection<AppDbContextFixture>]
public sealed class HallOfFameInductionPhotoPersistenceTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "A photo's focal point and zoom round-trip through the database")]
    public async Task Photo_ShouldRoundTripFocalPointAndZoom()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        var induction = HallOfFameInductionFactory.Create(
            bowlerId: bowler.Id,
            photo: StoredFileFactory.Create(),
            photoFocalX: 0,
            photoFocalY: 100,
            photoZoom: 300);
        await _dbContext.HallOfFameInductions.AddAsync(induction, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await using var readContext = fixture.CreateDbContext();
        var loaded = await readContext.HallOfFameInductions.AsNoTracking().SingleAsync(i => i.Id == induction.Id, ct);

        // Assert
        loaded.PhotoFocalPoint.ShouldBe(PhotoFocalPointFactory.Create(0, 100, 300));
        loaded.Photo.ShouldNotBeNull();
    }

    [Fact(DisplayName = "An induction with no photo reads back with no focal point")]
    public async Task NoPhoto_ShouldReadBackWithNoFocalPoint()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        var induction = HallOfFameInductionFactory.Create(bowlerId: bowler.Id);
        await _dbContext.HallOfFameInductions.AddAsync(induction, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await using var readContext = fixture.CreateDbContext();
        var loaded = await readContext.HallOfFameInductions.AsNoTracking().SingleAsync(i => i.Id == induction.Id, ct);

        // Assert
        loaded.Photo.ShouldBeNull();
        loaded.PhotoFocalPoint.ShouldBeNull();
    }

    [Fact(DisplayName = "Clearing the photo clears the focal point in the database")]
    public async Task ClearPhoto_ShouldClearFocalPointInDatabase()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        var induction = HallOfFameInductionFactory.Create(bowlerId: bowler.Id, photo: StoredFileFactory.Create());
        await _dbContext.HallOfFameInductions.AddAsync(induction, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        induction.ClearPhoto();
        await _dbContext.SaveChangesAsync(ct);

        // Assert
        await using var readContext = fixture.CreateDbContext();
        var loaded = await readContext.HallOfFameInductions.AsNoTracking().SingleAsync(i => i.Id == induction.Id, ct);
        loaded.Photo.ShouldBeNull();
        loaded.PhotoFocalPoint.ShouldBeNull();
    }
}