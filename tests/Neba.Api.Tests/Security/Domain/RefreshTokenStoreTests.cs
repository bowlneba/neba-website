using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Security.Domain;
using Neba.Api.Security.Register;
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

    [Fact(DisplayName = "StoreAsync then GetStoredJsonAsync round-trips the hashed token")]
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
        var stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson!);
        stored.ShouldNotBeNull();
        stored!.IssuedAt.ShouldBe(timeProvider.GetUtcNow());
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

    private static async Task<ApplicationUser> SeedUserAsync(UserManager<ApplicationUser> userManager)
    {
        var command = new RegisterCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = RegisterRequestFactory.ValidPassword
        };
        await new RegisterCommandHandler(userManager).HandleAsync(command, CancellationToken.None);

        var user = await userManager.FindByEmailAsync(command.Email);
        return user!;
    }
}