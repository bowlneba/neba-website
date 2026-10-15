using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.UpdateUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.UpdateUser;

[UnitTest]
[Component("Security")]
public sealed class UpdateUserEndpointTests
{
    private static Action<Microsoft.AspNetCore.Http.DefaultHttpContext> AsCaller(Ulid callerId, params string[] permissions)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, callerId.ToString()),
                .. permissions.Select(value => new Claim(Permissions.ClaimType, value))
            ],
            authenticationType: "test"));

    private static Mock<NebaMessaging.ICommandHandler<UpdateUserCommand, Updated>> CreateHandlerMock(
        ErrorOr<Updated> result, CancellationToken ct)
    {
        var mock = new Mock<NebaMessaging.ICommandHandler<UpdateUserCommand, Updated>>(MockBehavior.Strict);
        mock.Setup(h => h.HandleAsync(It.IsAny<UpdateUserCommand>(), ct)).ReturnsAsync(result);
        return mock;
    }

    [Fact(DisplayName = "HandleAsync should map the request and caller to the command and return 204 when the update succeeds")]
    public async Task HandleAsync_ShouldMapCommandAndReturn204_WhenUpdateSucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var callerId = Ulid.NewUlid();
        var request = UpdateUserRequestFactory.Create();

        var handlerMock = new Mock<NebaMessaging.ICommandHandler<UpdateUserCommand, Updated>>(MockBehavior.Strict);
        handlerMock
            .Setup(h => h.HandleAsync(
                It.Is<UpdateUserCommand>(c =>
                    c.UserId.ToString() == request.UserId
                    && c.AddedPermissions.SequenceEqual(request.User.AddedPermissions)
                    && c.RemovedPermissions.SequenceEqual(request.User.RemovedPermissions)
                    && c.CallerUserId == callerId
                    && c.CallerPermissions.SequenceEqual(new[] { Permissions.EditUser.Value })),
                ct))
            .ReturnsAsync(Result.Updated);

        var endpoint = Factory.Create<UpdateUserEndpoint>(AsCaller(callerId, Permissions.EditUser.Value), handlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        handlerMock.VerifyAll();
    }

    [Fact(DisplayName = "HandleAsync should return 401 when the caller's token has no valid subject")]
    public async Task HandleAsync_ShouldReturn401_WhenCallerHasNoSubject()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handlerMock = new Mock<NebaMessaging.ICommandHandler<UpdateUserCommand, Updated>>(MockBehavior.Strict);
        var endpoint = Factory.Create<UpdateUserEndpoint>(handlerMock.Object);

        // Act
        await endpoint.HandleAsync(UpdateUserRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        handlerMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the user does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenUserNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handlerMock = CreateHandlerMock(UpdateUserErrors.UserNotFound, ct);
        var endpoint = Factory.Create<UpdateUserEndpoint>(AsCaller(Ulid.NewUlid()), handlerMock.Object);

        // Act
        await endpoint.HandleAsync(UpdateUserRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 403 when the command is forbidden")]
    public async Task HandleAsync_ShouldReturn403_WhenCommandIsForbidden()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handlerMock = CreateHandlerMock(UpdateUserErrors.CannotEditOwnAccount, ct);
        var endpoint = Factory.Create<UpdateUserEndpoint>(AsCaller(Ulid.NewUlid()), handlerMock.Object);

        // Act
        await endpoint.HandleAsync(UpdateUserRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
    }

    [Fact(DisplayName = "HandleAsync should return 409 when the target is an Admin")]
    public async Task HandleAsync_ShouldReturn409_WhenTargetIsAdmin()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handlerMock = CreateHandlerMock(UpdateUserErrors.AdminOverridesNotAllowed, ct);
        var endpoint = Factory.Create<UpdateUserEndpoint>(AsCaller(Ulid.NewUlid()), handlerMock.Object);

        // Act
        await endpoint.HandleAsync(UpdateUserRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handlerMock = CreateHandlerMock(Error.Validation("UserPermissionOverrides.UnknownPermission", "Unknown."), ct);
        var endpoint = Factory.Create<UpdateUserEndpoint>(AsCaller(Ulid.NewUlid()), handlerMock.Object);

        // Act
        await endpoint.HandleAsync(UpdateUserRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected PUT route under /security")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute()
    {
        // Arrange
        var handlerMock = new Mock<NebaMessaging.ICommandHandler<UpdateUserCommand, Updated>>(MockBehavior.Strict);
        var endpoint = Factory.Create<UpdateUserEndpoint>(handlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("users/{userId}"));
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("security"));
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}