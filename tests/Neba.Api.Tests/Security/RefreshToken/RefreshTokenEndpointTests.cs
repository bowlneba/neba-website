using System.Globalization;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security.RefreshToken;
using Neba.Api.Security.Login;
using Neba.Api.Security.RefreshToken;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.RefreshToken;

[UnitTest]
[Component("Security")]
public sealed class RefreshTokenEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return 200 OK with mapped token response when refresh token is valid")]
    public async Task HandleAsync_ShouldReturn200WithMappedResponse_WhenRefreshTokenIsValid()
    {
        // Arrange
        var dto = LoginDtoFactory.Create(userId: Ulid.Parse("01000000000000000000000001", CultureInfo.InvariantCulture));
        var request = RefreshTokenRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RefreshTokenCommand, LoginDto>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<RefreshTokenCommand>(c =>
                    c.UserId == Ulid.Parse(request.UserId, CultureInfo.InvariantCulture)
                    && c.RefreshToken == request.RefreshToken),
                ct))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<RefreshTokenEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return 401 Unauthorized when refresh token is invalid")]
    public async Task HandleAsync_ShouldReturn401_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var request = RefreshTokenRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RefreshTokenCommand, LoginDto>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<RefreshTokenCommand>(), ct))
            .ReturnsAsync(RefreshTokenErrors.InvalidRefreshToken);

        var endpoint = Factory.Create<RefreshTokenEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
    }

    [Fact(DisplayName = "Configure should register anonymous POST route containing 'refresh'")]
    public void Configure_ShouldRegisterAnonymousPostRoute_ContainingRefresh()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RefreshTokenCommand, LoginDto>>(MockBehavior.Strict);
        var endpoint = Factory.Create<RefreshTokenEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("refresh"), "should include a 'refresh' route");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}
