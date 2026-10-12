using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.CreateUser;
using Neba.Api.Security.CreateUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.CreateUser;

[UnitTest]
[Component("Security")]
public sealed class CreateUserEndpointTests
{
    private static Action<Microsoft.AspNetCore.Http.DefaultHttpContext> AsCaller(params string[] permissions)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, Ulid.NewUlid().ToString()),
                .. permissions.Select(value => new Claim(Permissions.ClaimType, value))
            ],
            authenticationType: "test"));

    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when creation succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenCreationSucceeds()
    {
        // Arrange
        var userId = Ulid.NewUlid();
        var request = CreateUserRequestFactory.Create(
            roles: ["Webmaster", "Journalist"],
            usbcId: "12345",
            phoneNumber: "555-123-4567",
            addedPermissions: [Permissions.CreateArticle.Value],
            removedPermissions: [Permissions.DeleteArticle.Value]);
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, CreateUserResult>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<CreateUserCommand>(c =>
                    c.Email == request.User.Email
                    && c.Roles.SequenceEqual(request.User.Roles)
                    && c.UsbcId == request.User.UsbcId
                    && c.PhoneNumber == request.User.PhoneNumber
                    && c.AddedPermissions.SequenceEqual(request.User.AddedPermissions)
                    && c.RemovedPermissions.SequenceEqual(request.User.RemovedPermissions)
                    && c.CallerPermissions.SequenceEqual(new[] { Permissions.CreateArticle.Value })),
                ct))
            .ReturnsAsync(new CreateUserResult(userId, RolesAssigned: true, PermissionOverridesApplied: true));

        var endpoint = Factory.Create<CreateUserEndpoint>(AsCaller(Permissions.CreateArticle.Value), commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(201);
        endpoint.Response.UserId.ShouldBe(userId.ToString());
        endpoint.Response.RolesAssigned.ShouldBeTrue();
        endpoint.Response.PermissionOverridesApplied.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should surface RolesAssigned=false when role assignment failed")]
    public async Task HandleAsync_ShouldSurfaceRolesAssignedFalse_WhenRoleAssignmentFailed()
    {
        // Arrange
        var userId = Ulid.NewUlid();
        var request = CreateUserRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, CreateUserResult>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), ct))
            .ReturnsAsync(new CreateUserResult(userId, RolesAssigned: false, PermissionOverridesApplied: false));

        var endpoint = Factory.Create<CreateUserEndpoint>(AsCaller(Permissions.CreateArticle.Value), commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(201);
        endpoint.Response.RolesAssigned.ShouldBeFalse();
        endpoint.Response.PermissionOverridesApplied.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should return 403 Forbidden when the caller tries to grant a permission they do not hold")]
    public async Task HandleAsync_ShouldReturn403_WhenCommandReturnsForbidden()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create(addedPermissions: [Permissions.ClearCache.Value]);
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, CreateUserResult>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), ct))
            .ReturnsAsync(CreateUserErrors.PermissionNotHeld([Permissions.ClearCache.Value]));

        var endpoint = Factory.Create<CreateUserEndpoint>(AsCaller(), commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
    }

    [Fact(DisplayName = "HandleAsync should return 401 Unauthorized when the caller's token has no valid subject")]
    public async Task HandleAsync_ShouldReturn401_WhenCallerHasNoSubject()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, CreateUserResult>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateUserEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        commandHandlerMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "HandleAsync should return 409 Conflict when the email is already registered")]
    public async Task HandleAsync_ShouldReturn409_WhenEmailAlreadyRegistered()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, CreateUserResult>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), ct))
            .ReturnsAsync(CreateUserErrors.DuplicateEmail);

        var endpoint = Factory.Create<CreateUserEndpoint>(AsCaller(Permissions.CreateArticle.Value), commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, CreateUserResult>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), ct))
            .ReturnsAsync(Error.Validation("CreateUser.InvalidEmail", "Email is invalid."));

        var endpoint = Factory.Create<CreateUserEndpoint>(AsCaller(Permissions.CreateArticle.Value), commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /security")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderSecurityPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, CreateUserResult>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateUserEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("users"), "should register the users route");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("security"), "should be under the /security path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}