using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.UpdateUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.UpdateUser;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class UpdateUserCommandHandlerTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private async Task<ApplicationUser> SeedUserAsync(
        string email = "target@bowlneba.com", string? role = null, params Claim[] claims)
    {
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var user = ApplicationUserFactory.Create(userName: email, email: email);
        await userManager.CreateAsync(user);

        if (role is not null)
        {
            if (await roleManager.FindByNameAsync(role) is null)
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }

            await userManager.AddToRoleAsync(user, role);
        }

        if (claims.Length > 0)
        {
            await userManager.AddClaimsAsync(user, claims);
        }

        return user;
    }

    private async Task<List<(string Type, string Value)>> StoredClaimsAsync(Ulid userId, CancellationToken ct)
    {
        await using var context = fixture.CreateDbContext();
        var rows = await context.UserClaims.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(ct);
        return [.. rows.Select(c => (c.ClaimType!, c.ClaimValue!))];
    }

    private readonly FakeLogger<UpdateUserCommandHandler> _logger = new();

    private UpdateUserCommandHandler CreateHandler()
        => new(fixture.CreateDbContext(), _logger);

    private static UpdateUserCommand CreateCommand(
        Ulid userId,
        IReadOnlyCollection<string>? added = null,
        IReadOnlyCollection<string>? removed = null,
        Ulid? callerId = null,
        IReadOnlyCollection<string>? callerPermissions = null)
        => new()
        {
            UserId = userId,
            AddedPermissions = added ?? [],
            RemovedPermissions = removed ?? [],
            CallerUserId = callerId ?? Ulid.NewUlid(),
            CallerPermissions = callerPermissions ?? [.. Permissions.List.Select(p => p.Value)]
        };

    [Fact(DisplayName = "HandleAsync returns CannotEditOwnAccount when the caller is the target")]
    public async Task HandleAsync_ShouldReturnForbidden_WhenCallerIsTarget()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync();

        // Act
        var result = await CreateHandler().HandleAsync(CreateCommand(user.Id, callerId: user.Id), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(UpdateUserErrors.CannotEditOwnAccount);
    }

    [Fact(DisplayName = "HandleAsync returns UserNotFound when the target does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await CreateHandler().HandleAsync(CreateCommand(Ulid.NewUlid()), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(UpdateUserErrors.UserNotFound);
    }

    [Fact(DisplayName = "HandleAsync returns AdminOverridesNotAllowed when the target is an Admin")]
    public async Task HandleAsync_ShouldReturnConflict_WhenTargetIsAdmin()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var admin = await SeedUserAsync(role: Roles.Admin);

        // Act
        var result = await CreateHandler().HandleAsync(
            CreateCommand(admin.Id, removed: [Permissions.CreateArticle.Value]), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(UpdateUserErrors.AdminOverridesNotAllowed);
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when a permission is unknown")]
    public async Task HandleAsync_ShouldReturnValidationError_WhenPermissionIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync();

        // Act
        var result = await CreateHandler().HandleAsync(CreateCommand(user.Id, added: ["Not.Real"]), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
    }

    [Fact(DisplayName = "HandleAsync returns PermissionNotHeld naming a newly added permission the caller does not hold")]
    public async Task HandleAsync_ShouldReturnForbidden_WhenAddedPermissionIsNotHeld()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync();

        // Act
        var result = await CreateHandler().HandleAsync(
            CreateCommand(user.Id, added: [Permissions.ClearCache.Value], callerPermissions: [Permissions.EditUser.Value]), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Forbidden);
        result.FirstError.Description.ShouldContain(Permissions.ClearCache.Value);
        (await StoredClaimsAsync(user.Id, ct)).ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns PermissionNotHeld when lifting a removal of a permission the caller does not hold")]
    public async Task HandleAsync_ShouldReturnForbidden_WhenLiftedRemovalIsNotHeld()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync(claims: new Claim(Permissions.DeniedClaimType, Permissions.ClearCache.Value));

        // Act
        var result = await CreateHandler().HandleAsync(
            CreateCommand(user.Id, callerPermissions: [Permissions.EditUser.Value]), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Forbidden);
        (await StoredClaimsAsync(user.Id, ct)).ShouldContain((Permissions.DeniedClaimType, Permissions.ClearCache.Value));
    }

    [Fact(DisplayName = "HandleAsync succeeds when an existing added permission the caller does not hold is left unchanged")]
    public async Task HandleAsync_ShouldSucceed_WhenUnheldAddedPermissionIsUnchanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync(claims: new Claim(Permissions.ClaimType, Permissions.ClearCache.Value));

        // Act
        var result = await CreateHandler().HandleAsync(
            CreateCommand(
                user.Id,
                added: [Permissions.ClearCache.Value],
                removed: [Permissions.CreateArticle.Value],
                callerPermissions: [Permissions.EditUser.Value]),
            ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await StoredClaimsAsync(user.Id, ct);
        stored.ShouldContain((Permissions.ClaimType, Permissions.ClearCache.Value));
        stored.ShouldContain((Permissions.DeniedClaimType, Permissions.CreateArticle.Value));

        var log = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        log.Level.ShouldBe(LogLevel.Information);
        log.Message.ShouldContain($"added [{Permissions.ClearCache.Value}]");
        log.Message.ShouldContain($"removed [{Permissions.CreateArticle.Value}]");
    }

    [Fact(DisplayName = "HandleAsync does not log when the requested overrides match the stored overrides")]
    public async Task HandleAsync_ShouldNotLog_WhenOverridesAreUnchanged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync(claims: new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value));

        // Act
        var result = await CreateHandler().HandleAsync(
            CreateCommand(user.Id, added: [Permissions.CreateArticle.Value]), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        _logger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync stores the requested set and removes values that are no longer requested")]
    public async Task HandleAsync_ShouldApplyDiff()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync(
            "target@bowlneba.com",
            null,
            new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value),
            new Claim(Permissions.DeniedClaimType, Permissions.EditArticle.Value));

        // Act
        var result = await CreateHandler().HandleAsync(
            CreateCommand(
                user.Id,
                added: [Permissions.CreateSponsor.Value],
                removed: [Permissions.EditArticle.Value, Permissions.DeleteArticle.Value]),
            ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await StoredClaimsAsync(user.Id, ct);
        stored.Count.ShouldBe(3);
        stored.ShouldContain((Permissions.ClaimType, Permissions.CreateSponsor.Value));
        stored.ShouldContain((Permissions.DeniedClaimType, Permissions.EditArticle.Value));
        stored.ShouldContain((Permissions.DeniedClaimType, Permissions.DeleteArticle.Value));
    }

    [Fact(DisplayName = "HandleAsync clears every stored override when both sets are empty")]
    public async Task HandleAsync_ShouldClearOverrides_WhenSetsAreEmpty()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync(
            "target@bowlneba.com",
            null,
            new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value),
            new Claim(Permissions.DeniedClaimType, Permissions.EditArticle.Value));

        // Act
        var result = await CreateHandler().HandleAsync(CreateCommand(user.Id), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        (await StoredClaimsAsync(user.Id, ct)).ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync deletes a stored override whose value is no longer a known permission")]
    public async Task HandleAsync_ShouldDeleteRetiredStoredValue()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync(claims: new Claim(Permissions.ClaimType, "Retired.Permission"));

        // Act
        var result = await CreateHandler().HandleAsync(
            CreateCommand(user.Id, added: [Permissions.CreateArticle.Value]), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var stored = await StoredClaimsAsync(user.Id, ct);
        stored.ShouldBe([(Permissions.ClaimType, Permissions.CreateArticle.Value)]);
    }

    [Fact(DisplayName = "HandleAsync leaves other users' claims and non-permission claims alone")]
    public async Task HandleAsync_ShouldNotTouchOtherClaims()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var user = await SeedUserAsync(claims: new Claim("something-else", "keep"));
        var other = await SeedUserAsync(
            email: "other@bowlneba.com", claims: new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value));

        // Act
        var result = await CreateHandler().HandleAsync(CreateCommand(user.Id), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        (await StoredClaimsAsync(user.Id, ct)).ShouldBe([("something-else", "keep")]);
        (await StoredClaimsAsync(other.Id, ct)).ShouldBe([(Permissions.ClaimType, Permissions.CreateArticle.Value)]);
    }
}