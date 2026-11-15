using System.Globalization;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security.SetPasswordFromToken;
using Neba.Api.Security.Password.SetPasswordFromToken;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.Password.SetPasswordFromToken;

[UnitTest]
[Component("Security")]
public sealed class SetPasswordFromTokenEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return 204 NoContent when command succeeds")]
    public async Task HandleAsync_ShouldReturn204_WhenCommandSucceeds()
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<SetPasswordFromTokenCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<SetPasswordFromTokenCommand>(c => c.UserId == Ulid.Parse(request.UserId, CultureInfo.InvariantCulture)),
                ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<SetPasswordFromTokenEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Theory(DisplayName = "HandleAsync should pass the request's purpose to the command unchanged")]
    [InlineData(null, null)]
    [InlineData("reset", "reset")]
    [InlineData("invite", "invite")]
    public async Task HandleAsync_ShouldPassPurposeToCommand(string? purpose, string? expected)
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create(purpose: purpose);
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<SetPasswordFromTokenCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.Is<SetPasswordFromTokenCommand>(c => c.Purpose == expected), ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<SetPasswordFromTokenEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        commandHandlerMock.VerifyAll();
    }

    [Fact(DisplayName = "HandleAsync should return 422 when command returns failure errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsFailureErrors()
    {
        // Arrange
        var request = SetPasswordFromTokenRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<SetPasswordFromTokenCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<SetPasswordFromTokenCommand>(), ct))
            .ReturnsAsync(SetPasswordFromTokenErrors.InvalidOrExpiredToken);

        var endpoint = Factory.Create<SetPasswordFromTokenEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register anonymous POST route containing 'password/set-from-token'")]
    public void Configure_ShouldRegisterAnonymousPostRoute_ContainingPasswordSetFromToken()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<SetPasswordFromTokenCommand>>(MockBehavior.Strict);
        var endpoint = Factory.Create<SetPasswordFromTokenEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("password/set-from-token"), "should include a 'password/set-from-token' route");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}