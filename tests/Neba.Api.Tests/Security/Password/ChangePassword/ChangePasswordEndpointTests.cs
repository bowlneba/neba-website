using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security.ChangePassword;
using Neba.Api.Security.Password.ChangePassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.Password.ChangePassword;

[UnitTest]
[Component("Security")]
public sealed class ChangePasswordEndpointTests
{
    private static readonly Ulid CallerId = Ulid.Parse("01000000000000000000000001", CultureInfo.InvariantCulture);

    private static ChangePasswordEndpoint CreateEndpoint(
        Mock<NebaMessaging.ICommandHandler<ChangePasswordCommand, ChangePasswordDto>> handler,
        Ulid? callerId = null)
    {
        var endpoint = Factory.Create<ChangePasswordEndpoint>(handler.Object);

        if (callerId is not null)
        {
            endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, callerId.Value.ToString()),
            ]));
        }

        return endpoint;
    }

    [Fact(DisplayName = "HandleAsync should return 200 with the mapped tokens when the change succeeds")]
    public async Task HandleAsync_ShouldReturn200WithMappedResponse_WhenChangeSucceeds()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create();
        var dto = ChangePasswordDtoFactory.Create(userId: CallerId);
        var ct = TestContext.Current.CancellationToken;

        var handler = new Mock<NebaMessaging.ICommandHandler<ChangePasswordCommand, ChangePasswordDto>>(MockBehavior.Strict);
        handler
            .Setup(h => h.HandleAsync(
                It.Is<ChangePasswordCommand>(c =>
                    c.UserId == CallerId
                    && c.CurrentPassword == request.CurrentPassword
                    && c.NewPassword == request.NewPassword),
                ct))
            .ReturnsAsync(dto);

        var endpoint = CreateEndpoint(handler, CallerId);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var response = endpoint.Response;
        response.AccessToken.ShouldBe(dto.AccessToken);
        response.RefreshToken.ShouldBe(dto.RefreshToken);
        response.ExpiresAt.ShouldBe(dto.ExpiresAt);
        response.UserId.ShouldBe(dto.UserId.ToString());
        response.Email.ShouldBe(dto.Email);
    }

    [Fact(DisplayName = "HandleAsync should return 401 and not call the handler when the token has no user ID")]
    public async Task HandleAsync_ShouldReturn401_WhenTokenHasNoUserId()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;
        var handler = new Mock<NebaMessaging.ICommandHandler<ChangePasswordCommand, ChangePasswordDto>>(MockBehavior.Strict);
        var endpoint = CreateEndpoint(handler);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        handler.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "HandleAsync should return 401 when the user in the token no longer exists")]
    public async Task HandleAsync_ShouldReturn401_WhenUserNotFound()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;
        var handler = new Mock<NebaMessaging.ICommandHandler<ChangePasswordCommand, ChangePasswordDto>>(MockBehavior.Strict);
        handler
            .Setup(h => h.HandleAsync(It.IsAny<ChangePasswordCommand>(), ct))
            .ReturnsAsync(ChangePasswordErrors.UserNotFound);
        var endpoint = CreateEndpoint(handler, CallerId);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
    }

    [Fact(DisplayName = "HandleAsync should return 409 when the account is locked out")]
    public async Task HandleAsync_ShouldReturn409_WhenLockedOut()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;
        var handler = new Mock<NebaMessaging.ICommandHandler<ChangePasswordCommand, ChangePasswordDto>>(MockBehavior.Strict);
        handler
            .Setup(h => h.HandleAsync(It.IsAny<ChangePasswordCommand>(), ct))
            .ReturnsAsync(ChangePasswordErrors.LockedOut);
        var endpoint = CreateEndpoint(handler, CallerId);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the current password is incorrect")]
    public async Task HandleAsync_ShouldReturn422_WhenCurrentPasswordIncorrect()
    {
        // Arrange
        var request = ChangePasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;
        var handler = new Mock<NebaMessaging.ICommandHandler<ChangePasswordCommand, ChangePasswordDto>>(MockBehavior.Strict);
        handler
            .Setup(h => h.HandleAsync(It.IsAny<ChangePasswordCommand>(), ct))
            .ReturnsAsync(ChangePasswordErrors.IncorrectCurrentPassword);
        var endpoint = CreateEndpoint(handler, CallerId);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
        endpoint.ValidationFailures.ShouldContain(f => f.ErrorMessage == "Current password is incorrect.");
    }
}