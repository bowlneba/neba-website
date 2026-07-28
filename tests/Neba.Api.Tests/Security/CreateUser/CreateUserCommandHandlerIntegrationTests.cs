using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Email;
using Neba.Api.Security;
using Neba.Api.Security.CreateUser;
using Neba.Api.Security.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Email;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.CreateUser;

[IntegrationTest]
[Component("Security")]
[Collection<SecurityDbContextFixture>]
public sealed class CreateUserCommandHandlerIntegrationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string BaseUrl = "https://bowlneba.com";

    private readonly Mock<IEmailSender> _emailSender = new(MockBehavior.Strict);
    private readonly WebsiteSettings _websiteSettings = new() { BaseUrl = BaseUrl };
    private readonly FakeLogger<CreateUserCommandHandler> _logger = new();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private CreateUserCommandHandler CreateHandler(UserManager<ApplicationUser> userManager)
        => new(userManager, _emailSender.Object, _websiteSettings, _logger);

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, params IReadOnlyCollection<string> roles)
    {
        foreach (var role in roles)
        {
            if (await roleManager.FindByNameAsync(role) is null)
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }
    }

    [Fact(DisplayName = "HandleAsync returns a non-empty Ulid when creation succeeds")]
    public async Task HandleAsync_ShouldReturnUserId_WhenCreationSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var command = new CreateUserCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Roles = [Roles.Member]
        };

        await SeedRolesAsync(roleManager, command.Roles);
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBe(Ulid.Empty);
    }

    [Fact(DisplayName = "HandleAsync persists the user with EmailConfirmed=false")]
    public async Task HandleAsync_ShouldPersistUser_WithEmailConfirmedFalse()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var command = new CreateUserCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            UsbcId = "12345",
            PhoneNumber = "555-123-4567",
            Roles = [Roles.Member]
        };

        await SeedRolesAsync(roleManager, command.Roles);
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var stored = await userManager.FindByEmailAsync(command.Email);
        stored.ShouldNotBeNull();
        stored.EmailConfirmed.ShouldBeFalse();
        stored.Email.ShouldBe(command.Email);
        stored.UserName.ShouldBe(command.Email);
        stored.UsbcId.ShouldBe(command.UsbcId);
        stored.PhoneNumber.ShouldBe(command.PhoneNumber);
    }

    [Fact(DisplayName = "HandleAsync assigns the requested roles to the user")]
    public async Task HandleAsync_ShouldAssignRoles_WhenCreationSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var command = new CreateUserCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Roles = [Roles.Member, Roles.Journalist]
        };

        await SeedRolesAsync(roleManager, command.Roles);
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var stored = await userManager.FindByEmailAsync(command.Email);
        stored.ShouldNotBeNull();
        var roles = await userManager.GetRolesAsync(stored);
        roles.ShouldBe([Roles.Member, Roles.Journalist], ignoreOrder: true);
    }

    [Fact(DisplayName = "HandleAsync logs an error and still succeeds when role assignment fails")]
    public async Task HandleAsync_ShouldLogErrorAndStillSucceed_WhenRoleAssignmentFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // "MEMBER" normalizes to the same role as "Member" but is a distinct string, so
        // UserManager.AddToRolesAsync's Distinct() doesn't dedupe it — the second entry fails
        // with "UserAlreadyInRole" once the first has been assigned, exercising the real
        // IdentityResult failure path without mocking UserManager.
        var command = new CreateUserCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Roles = [Roles.Member, Roles.Member.ToUpperInvariant()]
        };

        await SeedRolesAsync(roleManager, Roles.Member);
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var logRecord = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        logRecord.Level.ShouldBe(LogLevel.Error);
        logRecord.Message.ShouldContain("Failed to assign role(s)");
        logRecord.Message.ShouldContain(result.Value.ToString());
    }

    [Fact(DisplayName = "HandleAsync assigns claims to the user when claims are provided")]
    public async Task HandleAsync_ShouldAssignClaims_WhenClaimsAreProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var command = new CreateUserCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Roles = [Roles.Member],
            Claims = [("permission", "tournaments.manage")]
        };

        await SeedRolesAsync(roleManager, command.Roles);
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        var stored = await userManager.FindByEmailAsync(command.Email);
        stored.ShouldNotBeNull();
        var claims = await userManager.GetClaimsAsync(stored);
        claims.ShouldContain(c => c.Type == "permission" && c.Value == "tournaments.manage");
    }

    [Fact(DisplayName = "HandleAsync sends an invite email with a set-password link")]
    public async Task HandleAsync_ShouldSendInviteEmail_WithSetPasswordLink()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var command = new CreateUserCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Roles = [Roles.Member]
        };

        await SeedRolesAsync(roleManager, command.Roles);
        EmailMessage? sentMessage = null;
        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        sentMessage.ShouldNotBeNull();
        sentMessage.To.ShouldBe(command.Email);
        sentMessage.Subject.ShouldBe("You've been invited to BowlNEBA");
        sentMessage.HtmlBody.ShouldContain($"{BaseUrl}/account/set-password?userId={result.Value}&amp;token=");
    }

    [Fact(DisplayName = "HandleAsync returns DuplicateEmail conflict when email already exists")]
    public async Task HandleAsync_ShouldReturnDuplicateEmailError_WhenEmailAlreadyRegistered()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var command = new CreateUserCommand
        {
            Email = LoginRequestFactory.ValidEmail,
            Roles = [Roles.Member]
        };

        _emailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), ct))
            .Returns(Task.CompletedTask);

        using (var firstScope = fixture.CreateScope())
        {
            var firstManager = firstScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var firstRoleManager = firstScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            await SeedRolesAsync(firstRoleManager, command.Roles);
            await CreateHandler(firstManager).HandleAsync(command, ct);
        }

        // Act
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Conflict);
        result.FirstError.Code.ShouldBe("CreateUser.DuplicateEmail");
    }

    [Fact(DisplayName = "HandleAsync returns validation errors when email is invalid")]
    public async Task HandleAsync_ShouldReturnValidationErrors_WhenEmailIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var command = new CreateUserCommand
        {
            Email = "not-an-email",
            Roles = [Roles.Member]
        };

        // Act
        var result = await CreateHandler(userManager).HandleAsync(command, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldAllBe(e => e.Type == ErrorOr.ErrorType.Validation);
        result.Errors.ShouldNotBeEmpty();
    }
}