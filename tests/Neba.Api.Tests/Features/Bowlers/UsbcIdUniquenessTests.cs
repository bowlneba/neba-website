using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Features.Bowlers;

[IntegrationTest]
[Component("Bowlers")]
[Collection<AppDbContextFixture>]
public sealed class UsbcIdUniquenessTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "Saving two bowlers with the same USBC ID fails")]
    public async Task SaveChanges_ShouldThrow_WhenUsbcIdIsDuplicated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _dbContext.Bowlers.AddRangeAsync(
            [BowlerFactory.Create(usbcId: "1234-5678"), BowlerFactory.Create(usbcId: "1234-5678")],
            ct);

        // Act
        var save = () => _dbContext.SaveChangesAsync(ct);

        // Assert
        await Should.ThrowAsync<DbUpdateException>(save);
    }

    [Fact(DisplayName = "Saving bowlers with null or blank USBC IDs is allowed")]
    public async Task SaveChanges_ShouldSucceed_WhenUsbcIdsAreNullOrBlank()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _dbContext.Bowlers.AddRangeAsync(
            [
                BowlerFactory.Create(usbcId: null),
                BowlerFactory.Create(usbcId: null),
                BowlerFactory.Create(usbcId: string.Empty),
                BowlerFactory.Create(usbcId: string.Empty)
            ],
            ct);

        // Act
        var saved = await _dbContext.SaveChangesAsync(ct);

        // Assert
        saved.ShouldBeGreaterThanOrEqualTo(4);
    }
}