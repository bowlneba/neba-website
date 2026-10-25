using System.Net;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Security.Password.ForgotPassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.Password.ForgotPassword;

[UnitTest]
[Component("Security")]
public sealed class ForgotPasswordEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return 204 NoContent and pass the requester's IP and user agent to the command")]
    public async Task HandleAsync_ShouldReturn204AndPassRequesterDetails()
    {
        // Arrange
        var request = ForgotPasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ForgotPasswordCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<ForgotPasswordCommand>(c =>
                    c.Email == request.Email
                    && c.RequesterIp == "203.0.113.7"
                    && c.UserAgent == "TestAgent/1.0"),
                ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<ForgotPasswordEndpoint>(commandHandlerMock.Object);
        endpoint.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        endpoint.HttpContext.Request.Headers.UserAgent = "TestAgent/1.0";

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        commandHandlerMock.VerifyAll();
    }

    [Fact(DisplayName = "HandleAsync should return 204 NoContent when the requester's IP is unknown")]
    public async Task HandleAsync_ShouldReturn204_WhenRequesterIpIsUnknown()
    {
        // Arrange
        var request = ForgotPasswordRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ForgotPasswordCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.Is<ForgotPasswordCommand>(c => c.RequesterIp == null), ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<ForgotPasswordEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }
}