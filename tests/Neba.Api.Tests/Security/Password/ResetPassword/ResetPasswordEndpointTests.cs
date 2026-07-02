using System.Globalization;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security.ResetPassword;
using Neba.Api.Security.Password.ResetPassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.Password.ResetPassword;

[UnitTest]
[Component("Security")]
public sealed class ResetPasswordEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return 204 NoContent when command succeeds")]
    public async Task HandleAsync_ShouldReturn204_WhenCommandSucceeds()
    {
        // Arrange
        var request = ResetPasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ResetPasswordCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<ResetPasswordCommand>(c => c.UserId == Ulid.Parse(request.UserId, CultureInfo.InvariantCulture)),
                ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<ResetPasswordEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when user is not found")]
    public async Task HandleAsync_ShouldReturn404_WhenUserNotFound()
    {
        // Arrange
        var request = ResetPasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ResetPasswordCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ResetPasswordCommand>(), ct))
            .ReturnsAsync(ResetPasswordErrors.UserNotFound);

        var endpoint = Factory.Create<ResetPasswordEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when command returns failure errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsFailureErrors()
    {
        // Arrange
        var request = ResetPasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ResetPasswordCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ResetPasswordCommand>(), ct))
            .ReturnsAsync(Error.Failure("Identity.PasswordFailure", "Failed to update password."));

        var endpoint = Factory.Create<ResetPasswordEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register authenticated POST route containing 'password/reset'")]
    public void Configure_ShouldRegisterAuthenticatedPostRoute_ContainingPasswordReset()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ResetPasswordCommand>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ResetPasswordEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("password/reset"), "should include a 'password/reset' route");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}