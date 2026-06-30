using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;
using Neba.Website.Server.Services;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services.BearerTokenHandler")]
public sealed class BearerTokenHandlerTests
{
    private static readonly Uri ApiBaseUrl = new("https://api.example.com");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact(DisplayName = "Should attach the access token as a bearer header when present")]
    public async Task SendAsync_ShouldAttachBearerHeader_WhenAccessTokenPresent()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpContextAccessorMock = CreateAccessor(BuildHttpContext(accessToken: "old-token"));
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "old-token"));
    }

    [Fact(DisplayName = "Should not attach a bearer header when there is no HttpContext")]
    public async Task SendAsync_ShouldNotAttachBearerHeader_WhenNoHttpContext()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBeNull();
    }

    [Fact(DisplayName = "Should silently refresh and retry when the response is Unauthorized")]
    public async Task SendAsync_ShouldRefreshAndRetry_WhenResponseIsUnauthorizedAndRefreshSucceeds()
    {
        // Arrange
        var responses = new Queue<HttpStatusCode>([HttpStatusCode.Unauthorized, HttpStatusCode.OK]);
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(responses.Dequeue()));

        using var refreshHandler = new RecordingHandler(_ =>
        {
            var json = JsonSerializer.Serialize(new
            {
                accessToken = "new-token",
                refreshToken = "new-refresh",
                expiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
                userId = "user-123",
                email = "admin@bowlneba.com",
            }, JsonOptions);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var authServiceMock = new Mock<IAuthenticationService>(MockBehavior.Strict);
        authServiceMock
            .Setup(s => s.SignInAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var httpContext = BuildHttpContext(accessToken: "old-token", refreshToken: "refresh-abc", userId: "user-123", authServiceMock: authServiceMock);

        var httpContextAccessorMock = CreateAccessor(httpContext);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.Count.ShouldBe(2);
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "old-token"));
        innerHandler.Requests[1].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "new-token"));
        refreshHandler.Requests.ShouldHaveSingleItem();
        refreshHandler.Requests[0].RequestUri.ShouldBe(new Uri(ApiBaseUrl, "/security/refresh"));
        authServiceMock.VerifyAll();
    }

    [Fact(DisplayName = "Should return the original Unauthorized response when the refresh token is missing")]
    public async Task SendAsync_ShouldReturnOriginalUnauthorized_WhenRefreshTokenMissing()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var httpContext = BuildHttpContext(accessToken: "old-token", refreshToken: null, userId: "user-123");
        var httpContextAccessorMock = CreateAccessor(httpContext);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        innerHandler.Requests.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Should return the original Unauthorized response when the refresh call fails")]
    public async Task SendAsync_ShouldReturnOriginalUnauthorized_WhenRefreshCallFails()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var refreshHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var httpContext = BuildHttpContext(accessToken: "old-token", refreshToken: "refresh-abc", userId: "user-123");
        var httpContextAccessorMock = CreateAccessor(httpContext);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        innerHandler.Requests.ShouldHaveSingleItem();
        refreshHandler.Requests.ShouldHaveSingleItem();
    }

    private static BearerTokenHandler CreateHandler(
        HttpMessageHandler innerHandler,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory)
    {
        var handler = new BearerTokenHandler(
            httpContextAccessor,
            httpClientFactory,
            new NebaApiConfiguration { BaseUrl = ApiBaseUrl },
            NullLogger<BearerTokenHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        return handler;
    }

    private static Mock<IHttpContextAccessor> CreateAccessor(HttpContext httpContext)
    {
        var mock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        mock.SetupGet(a => a.HttpContext).Returns(httpContext);
        return mock;
    }

    private static DefaultHttpContext BuildHttpContext(
        string? accessToken,
        string? refreshToken = null,
        string? userId = null,
        Mock<IAuthenticationService>? authServiceMock = null)
    {
        var claims = new List<Claim>();

        if (userId is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        var tokens = new List<AuthenticationToken>();
        if (accessToken is not null)
            tokens.Add(new AuthenticationToken { Name = SecurityClaimsBuilder.AccessTokenName, Value = accessToken });
        if (refreshToken is not null)
            tokens.Add(new AuthenticationToken { Name = SecurityClaimsBuilder.RefreshTokenName, Value = refreshToken });

        var properties = new AuthenticationProperties();
        properties.StoreTokens(tokens);

        var httpContext = new DefaultHttpContext { User = principal };

        var auth = authServiceMock ?? new Mock<IAuthenticationService>(MockBehavior.Strict);
        auth.Setup(s => s.AuthenticateAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme)));

        httpContext.RequestServices = new ServiceCollection().AddSingleton(auth.Object).BuildServiceProvider();

        return httpContext;
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}