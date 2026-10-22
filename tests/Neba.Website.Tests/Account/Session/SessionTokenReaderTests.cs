using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account.Session;

namespace Neba.Website.Tests.Account.Session;

[UnitTest]
[Component("Website.Account.Session")]
public sealed class SessionTokenReaderTests
{
    [Fact(DisplayName = "ReadAsync should return the user, email, and both tokens from the cookie")]
    public async Task ReadAsync_ShouldReturnTokens_WhenCookieHoldsThem()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build("user-123", "access-1", "refresh-1");

        // Act
        var result = await SessionTokenReader.ReadAsync(httpContext);

        // Assert
        result.ShouldBe(new SessionTokens("user-123", "admin@bowlneba.com", "access-1", "refresh-1"));
    }

    [Fact(DisplayName = "ReadAsync should return null when the request has no signed-in user")]
    public async Task ReadAsync_ShouldReturnNull_WhenNoUserId()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build(userId: null, "access-1", "refresh-1");

        // Act
        var result = await SessionTokenReader.ReadAsync(httpContext);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "ReadAsync should return null when the cookie has no access token")]
    public async Task ReadAsync_ShouldReturnNull_WhenNoAccessToken()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build("user-123", accessToken: null, "refresh-1");

        // Act
        var result = await SessionTokenReader.ReadAsync(httpContext);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "ReadAsync should return null when the cookie has no refresh token")]
    public async Task ReadAsync_ShouldReturnNull_WhenNoRefreshToken()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build("user-123", "access-1", refreshToken: null);

        // Act
        var result = await SessionTokenReader.ReadAsync(httpContext);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "ReadAsync should use an empty email when the user has no email claim")]
    public async Task ReadAsync_ShouldUseEmptyEmail_WhenNoEmailClaim()
    {
        // Arrange
        var httpContext = SessionHttpContext.Build("user-123", "access-1", "refresh-1", email: null);

        // Act
        var result = await SessionTokenReader.ReadAsync(httpContext);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBeEmpty();
    }
}