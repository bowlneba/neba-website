using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Security.GetCurrentUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.GetCurrentUser;

[UnitTest]
[Component("Security")]
public sealed class GetCurrentUserEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return 200 OK with mapped MeResponse when user is found")]
    public async Task HandleAsync_ShouldReturn200WithMappedResponse_WhenUserIsFound()
    {
        // Arrange
        var userId = Ulid.Parse("01000000000000000000000001", CultureInfo.InvariantCulture);
        var dto = UserDtoFactory.Create(userId: userId);
        var ct = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<NebaMessaging.IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<GetCurrentUserQuery>(q => q.UserId == userId),
                ct))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetCurrentUserEndpoint>(queryHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        ]));

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return 200 OK when user ID is in NameIdentifier claim")]
    public async Task HandleAsync_ShouldReturn200_WhenUserIdIsInNameIdentifierClaim()
    {
        // Arrange
        var userId = Ulid.Parse("01000000000000000000000002", CultureInfo.InvariantCulture);
        var dto = UserDtoFactory.Create(userId: userId);
        var ct = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<NebaMessaging.IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<GetCurrentUserQuery>(q => q.UserId == userId),
                ct))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetCurrentUserEndpoint>(queryHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        ]));

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
    }

    [Fact(DisplayName = "HandleAsync should return 401 Unauthorized when no user ID claim is present")]
    public async Task HandleAsync_ShouldReturn401_WhenNoUserIdClaimIsPresent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<NebaMessaging.IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>>>(MockBehavior.Strict);

        var endpoint = Factory.Create<GetCurrentUserEndpoint>(queryHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
        queryHandlerMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "HandleAsync should return 404 NotFound when user does not exist in Identity")]
    public async Task HandleAsync_ShouldReturn404_WhenUserDoesNotExistInIdentity()
    {
        // Arrange
        var userId = Ulid.Parse("01000000000000000000000003", CultureInfo.InvariantCulture);
        var ct = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<NebaMessaging.IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetCurrentUserQuery>(), ct))
            .ReturnsAsync(GetCurrentUserErrors.UserNotFound);

        var endpoint = Factory.Create<GetCurrentUserEndpoint>(queryHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        ]));

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "Configure should register authenticated GET route containing 'me'")]
    public void Configure_ShouldRegisterAuthenticatedGetRoute_ContainingMe()
    {
        // Arrange
        var queryHandlerMock = new Mock<NebaMessaging.IQueryHandler<GetCurrentUserQuery, ErrorOr<UserDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetCurrentUserEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("me"), "should include a 'me' route");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}