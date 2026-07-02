using System.Globalization;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security.Login;
using Neba.Api.Security.Login;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.Login;

[UnitTest]
[Component("Security")]
public sealed class LoginEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return 200 OK with mapped token response when credentials are valid")]
    public async Task HandleAsync_ShouldReturn200WithMappedResponse_WhenCredentialsAreValid()
    {
        // Arrange
        var dto = LoginDtoFactory.Create(userId: Ulid.Parse("01000000000000000000000001", CultureInfo.InvariantCulture));
        var request = LoginRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<LoginCommand, LoginDto>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<LoginCommand>(c => c.Email == request.Email && c.Password == request.Password),
                ct))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<LoginEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return 401 Unauthorized when credentials are invalid")]
    public async Task HandleAsync_ShouldReturn401_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = LoginRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<LoginCommand, LoginDto>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<LoginCommand>(), ct))
            .ReturnsAsync(LoginErrors.InvalidCredentials);

        var endpoint = Factory.Create<LoginEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
    }

    [Fact(DisplayName = "Configure should register anonymous POST route containing 'login'")]
    public void Configure_ShouldRegisterAnonymousPostRoute_ContainingLogin()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<LoginCommand, LoginDto>>(MockBehavior.Strict);
        var endpoint = Factory.Create<LoginEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("login"), "should include a 'login' route");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}