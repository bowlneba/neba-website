using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Security.Domain;
using Neba.Api.Security.Register;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.Register;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class RegisterCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    [Fact(DisplayName = "HandleAsync returns a non-empty Ulid when registration succeeds")]
    public async Task HandleAsync_ShouldReturnUserId_WhenRegistrationSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new RegisterCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = RegisterRequestFactory.ValidPassword
        };

        // Act
        var result = await new RegisterCommandHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBe(Ulid.Empty);
    }

    [Fact(DisplayName = "HandleAsync persists the user with EmailConfirmed=true")]
    public async Task HandleAsync_ShouldPersistUser_WithEmailConfirmedTrue()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new RegisterCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = RegisterRequestFactory.ValidPassword
        };

        // Act
        await new RegisterCommandHandler(userManager).HandleAsync(command, ct);

        // Assert
        var stored = await userManager.FindByEmailAsync(command.Email);
        stored.ShouldNotBeNull();
        stored.EmailConfirmed.ShouldBeTrue();
        stored.Email.ShouldBe(command.Email);
        stored.UserName.ShouldBe(command.Email);
    }

    [Fact(DisplayName = "HandleAsync returns DuplicateEmail conflict when email already exists")]
    public async Task HandleAsync_ShouldReturnDuplicateEmailError_WhenEmailAlreadyRegistered()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var command = new RegisterCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = RegisterRequestFactory.ValidPassword
        };

        using (var firstScope = fixture.CreateScope())
        {
            var firstManager = firstScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await new RegisterCommandHandler(firstManager).HandleAsync(command, ct);
        }

        // Act
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await new RegisterCommandHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("Register.DuplicateEmail");
    }

    [Fact(DisplayName = "HandleAsync returns validation errors when password does not meet requirements")]
    public async Task HandleAsync_ShouldReturnValidationErrors_WhenPasswordTooWeak()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new RegisterCommand
        {
            Email = RegisterRequestFactory.ValidEmail,
            Password = "short"
        };

        // Act
        var result = await new RegisterCommandHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldAllBe(e => e.Type == ErrorOr.ErrorType.Validation);
        result.Errors.ShouldNotBeEmpty();
    }
}