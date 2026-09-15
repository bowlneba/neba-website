using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Domain;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class RefreshTokenStoreTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    [Fact(DisplayName = "StoreAsync then GetStoredJsonAsync round-trips a single, fully-valid slot")]
    public async Task StoreAsync_ShouldRoundTrip_ThroughGetStoredJsonAsync()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        // Act
        await RefreshTokenStore.StoreAsync(userManager, user, "raw-refresh-token", timeProvider);

        // Assert
        var storedJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
        storedJson.ShouldNotBeNullOrEmpty();
        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson);
        stored.ShouldNotBeNull();
        stored.IssuedAt.ShouldBe(timeProvider.GetUtcNow());
        var slot = stored.Slots.ShouldHaveSingleItem();
        slot.Hash.ShouldBe(RefreshTokenStore.ComputeHash("raw-refresh-token"));
        slot.GracedUntil.ShouldBeNull();
    }

    [Fact(DisplayName = "StoreAsync replaces any previously-stored slots")]
    public async Task StoreAsync_ShouldReplacePreviouslyStoredSlots()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await RefreshTokenStore.StoreAsync(userManager, user, "first-token", timeProvider);

        // Act
        await RefreshTokenStore.StoreAsync(userManager, user, "second-token", timeProvider);

        // Assert
        var storedJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson!);
        stored.ShouldNotBeNull();
        var slot = stored.Slots.ShouldHaveSingleItem();
        slot.Hash.ShouldBe(RefreshTokenStore.ComputeHash("second-token"));
    }

    [Fact(DisplayName = "RemoveAsync clears the stored token")]
    public async Task RemoveAsync_ShouldClearStoredToken()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await SeedUserAsync(userManager);
        await RefreshTokenStore.StoreAsync(userManager, user, "raw-refresh-token", new FakeTimeProvider(DateTimeOffset.UtcNow));

        // Act
        await RefreshTokenStore.RemoveAsync(userManager, user);

        // Assert
        var storedJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
        storedJson.ShouldBeNull();
    }

    [Fact(DisplayName = "TryStoreAsync writes the new value and returns true when the expected JSON still matches what is stored")]
    public async Task TryStoreAsync_ShouldWriteAndReturnTrue_WhenExpectedJsonMatchesStoredValue()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var user = await SeedUserAsync(userManager);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await RefreshTokenStore.StoreAsync(userManager, user, "original-token", timeProvider);
        var expectedJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
        expectedJson.ShouldNotBeNullOrEmpty();
        var newValue = new StoredRefreshToken
        {
            Slots = [new TokenSlot { Hash = RefreshTokenStore.ComputeHash("new-token"), GracedUntil = null }],
            IssuedAt = timeProvider.GetUtcNow()
        };

        // Act
        var succeeded = await RefreshTokenStore.TryStoreAsync(securityDbContext, user, expectedJson, newValue);

        // Assert
        succeeded.ShouldBeTrue();
        var storedJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson!);
        stored.ShouldNotBeNull();
        stored.Slots.ShouldHaveSingleItem().Hash.ShouldBe(RefreshTokenStore.ComputeHash("new-token"));
    }

    [Fact(DisplayName = "TryStoreAsync leaves the stored token unchanged and returns false when the expected JSON is stale")]
    public async Task TryStoreAsync_ShouldNotWriteAndReturnFalse_WhenExpectedJsonIsStale()
    {
        // Arrange
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var securityDbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var user = await SeedUserAsync(userManager);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await RefreshTokenStore.StoreAsync(userManager, user, "original-token", timeProvider);
        var staleExpectedJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
        staleExpectedJson.ShouldNotBeNullOrEmpty();

        // Someone else already rotated the token in between - the caller's "expected" snapshot is stale.
        await RefreshTokenStore.StoreAsync(userManager, user, "someone-else-already-rotated-to-this-token", timeProvider);
        var winningJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
        var loserValue = new StoredRefreshToken
        {
            Slots = [new TokenSlot { Hash = RefreshTokenStore.ComputeHash("loser-token"), GracedUntil = null }],
            IssuedAt = timeProvider.GetUtcNow()
        };

        // Act
        var succeeded = await RefreshTokenStore.TryStoreAsync(securityDbContext, user, staleExpectedJson, loserValue);

        // Assert
        succeeded.ShouldBeFalse();
        var storedJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
        storedJson.ShouldBe(winningJson, "a stale CAS write must not clobber the value someone else already wrote");
    }

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var user = ApplicationUserFactory.Create(userName: LoginRequestFactory.ValidEmail, email: LoginRequestFactory.ValidEmail);
        user.EmailConfirmed = true;
        await userManager.CreateAsync(user, LoginRequestFactory.ValidPassword);
        return user;
    }
}
