using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;
using Neba.Website.Server.Account.Session;
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
        var cookieToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10));
        var httpContextAccessorMock = CreateAccessor(BuildHttpContext(accessToken: cookieToken, refreshToken: "refresh-abc", userId: "user-123"));
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", cookieToken));
    }

    [Fact(DisplayName = "Should not attach a bearer header when there is no HttpContext and no cached token")]
    public async Task SendAsync_ShouldNotAttachBearerHeader_WhenNoHttpContextAndNoCachedToken()
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

    [Fact(DisplayName = "Should attach the circuit's cached access token as a bearer header when there is no HttpContext")]
    public async Task SendAsync_ShouldAttachCachedBearerHeader_WhenNoHttpContext()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var circuitToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10));
        using var circuit = CreateCircuit(SeededCache(circuitToken));
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", circuitToken));
    }

    [Fact(DisplayName = "Should proactively refresh an expired cached access token before sending, without waiting for a 401")]
    public async Task SendAsync_ShouldProactivelyRefresh_WhenCachedAccessTokenIsExpired()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

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

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        var expiredToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-1));
        var tokenCache = SeededCache(expiredToken);
        using var circuit = CreateCircuit(tokenCache, CreateCookieSync(persistResult: true));
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "new-token"));
        tokenCache.AccessToken.ShouldBe("new-token");
    }

    [Fact(DisplayName = "Should not attempt a refresh when the cached access token is not expired")]
    public async Task SendAsync_ShouldNotRefresh_WhenCachedAccessTokenIsStillValid()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        // Strict with no setups - throws if a refresh is attempted.
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var validToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10));
        using var circuit = CreateCircuit(SeededCache(validToken));
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", validToken));
    }

    [Fact(DisplayName = "Should silently refresh, retry, and write the new pair to the browser cookie when inside a circuit")]
    public async Task SendAsync_ShouldRefreshAndRetryUsingCache_WhenNoHttpContextAndRefreshSucceeds()
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

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        var oldToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10));
        var tokenCache = SeededCache(oldToken);
        var cookieSync = CreateCookieSync(persistResult: true);
        using var circuit = CreateCircuit(tokenCache, cookieSync);
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.Count.ShouldBe(2);
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", oldToken));
        innerHandler.Requests[1].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "new-token"));
        tokenCache.AccessToken.ShouldBe("new-token");
        tokenCache.RefreshToken.ShouldBe("new-refresh");
        cookieSync.Verify(c => c.PersistAsync(It.Is<SessionTokens>(t => t.RefreshToken == "new-refresh"), It.IsAny<CancellationToken>()), Times.Once);
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

        var oldToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10));
        var httpContext = BuildHttpContext(accessToken: oldToken, refreshToken: "refresh-abc", userId: "user-123", authServiceMock: authServiceMock);

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
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", oldToken));
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

        var httpContext = BuildHttpContext(accessToken: BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10)), refreshToken: "refresh-abc", userId: "user-123");
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

    [Fact(DisplayName = "Should serialize concurrent refreshes through one call when multiple requests race an expiring cached token")]
    public async Task SendAsync_ShouldSingleFlightRefresh_WhenConcurrentRequestsRaceExpiringToken()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var newAccessToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(15));
        var refreshStarted = new TaskCompletionSource();
        var releaseRefresh = new TaskCompletionSource();
        var refreshCallCount = 0;

        // An async, gated responder so the test can deterministically pin the second call at the
        // shared SessionTokenCache.RefreshLock while the first call's refresh is still in flight -
        // exactly the race that produced concurrent /security/refresh calls in production.
        using var refreshHandler = new CapturingHandler(async _ =>
        {
            Interlocked.Increment(ref refreshCallCount);
            refreshStarted.TrySetResult();
#pragma warning disable VSTHRD003 // Deliberate cross-task gate to pin the timing of the race under test.
            await releaseRefresh.Task;
#pragma warning restore VSTHRD003

            var json = JsonSerializer.Serialize(new
            {
                accessToken = newAccessToken,
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

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        var expiredToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-1));
        // Shared across both handlers, matching two calls on the same Blazor circuit going through
        // separate transient BearerTokenHandler instances but the same circuit-scoped SessionTokenCache.
        var tokenCache = SeededCache(expiredToken);
        using var circuit = CreateCircuit(tokenCache, CreateCookieSync(persistResult: true));

        using var sutA = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var sutB = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var clientA = new HttpClient(sutA, disposeHandler: false);
        using var clientB = new HttpClient(sutB, disposeHandler: false);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var taskA = clientA.GetAsync(new Uri("https://downstream.example.com/resource"), ct);
        await refreshStarted.Task; // A is now holding the lock, mid-refresh.
        var taskB = clientB.GetAsync(new Uri("https://downstream.example.com/resource"), ct);
        await Task.Delay(50, ct); // Give B's SendAsync a chance to queue behind A on the lock.
        releaseRefresh.TrySetResult();

#pragma warning disable VSTHRD003 // Deliberately awaiting the two calls kicked off above, after fanning them out.
        using var responseA = await taskA;
        using var responseB = await taskB;
#pragma warning restore VSTHRD003

        // Assert
        responseA.StatusCode.ShouldBe(HttpStatusCode.OK);
        responseB.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshCallCount.ShouldBe(1, "B should reuse A's refreshed token instead of racing its own refresh call");
        innerHandler.Requests.Count.ShouldBe(2);
        innerHandler.Requests.ShouldAllBe(r => Equals(r.Headers.Authorization, new AuthenticationHeaderValue("Bearer", newAccessToken)));
        tokenCache.AccessToken.ShouldBe(newAccessToken);
    }

    [Fact(DisplayName = "Should retry with the original body intact when the request content is a single-read stream")]
    public async Task SendAsync_ShouldRetryWithOriginalBody_WhenContentIsSingleReadStream()
    {
        // Arrange
        var responses = new Queue<HttpStatusCode>([HttpStatusCode.Unauthorized, HttpStatusCode.OK]);
        var capturedBodies = new List<byte[]>();

        // Reads via CopyToAsync rather than ReadAsByteArrayAsync, since ReadAsByteArrayAsync
        // internally buffers/caches the content on first read — which would silently paper over
        // the bug under test (a single-read stream only surviving one real serialization pass).
        // This mirrors how the real network stack (SocketsHttpHandler) drains request content.
        using var innerHandler = new CapturingHandler(async request =>
        {
            await using var buffer = new MemoryStream();
            if (request.Content is not null)
                await request.Content.CopyToAsync(buffer, TestContext.Current.CancellationToken);
            capturedBodies.Add(buffer.ToArray());
            return new HttpResponseMessage(responses.Dequeue());
        });

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
            .Returns(Task.CompletedTask);

        var httpContext = BuildHttpContext(accessToken: BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10)), refreshToken: "refresh-abc", userId: "user-123", authServiceMock: authServiceMock);
        var httpContextAccessorMock = CreateAccessor(httpContext);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object);
        using var client = new HttpClient(sut, disposeHandler: false);

        var expectedBytes = "file-bytes"u8.ToArray();

        // Simulates IBrowserFile.OpenReadStream(): forward-only, single-read, non-seekable —
        // reading it a second time (as the pre-fix CloneRequestAsync retry did) yields nothing.
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://downstream.example.com/upload"))
        {
            Content = new StreamContent(new SingleReadStream(expectedBytes))
        };

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        capturedBodies.Count.ShouldBe(2);
        capturedBodies[1].ShouldBe(expectedBytes);
    }

    [Fact(DisplayName = "Should keep using the refreshed token on later circuit calls instead of the stale connection cookie")]
    public async Task SendAsync_ShouldReuseRefreshedToken_WhenLaterCircuitCallSeesStaleConnectionCookie()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var newAccessToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(15));

        // The API rotates refresh tokens: the first redemption of "refresh-0" succeeds, any later one fails.
        var redeemed = new HashSet<string>();
        using var refreshHandler = new CapturingHandler(async request =>
        {
            var body = await request.Content!.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            var presented = body.GetProperty("refreshToken").GetString()!;
            if (presented != "refresh-0" || !redeemed.Add(presented))
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);

            return RefreshResponse(newAccessToken, "refresh-1");
        });

        // Inside a live circuit the accessor returns the connection's HttpContext: its cookie never
        // changes and its response has already started.
        var connectionContext = BuildHttpContext(
            accessToken: BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-1)),
            refreshToken: "refresh-0",
            userId: "user-123",
            responseStarted: true);
        var httpContextAccessorMock = CreateAccessor(connectionContext);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        using var circuit = CreateCircuit(cookieSync: CreateCookieSync(persistResult: true));
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var first = await client.GetAsync(new Uri("https://downstream.example.com/first"), ct);
        using var second = await client.GetAsync(new Uri("https://downstream.example.com/second"), ct);

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.Count.ShouldBe(2);
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", newAccessToken));
        innerHandler.Requests[1].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", newAccessToken));
    }

    [Fact(DisplayName = "Should never send one circuit's refreshed token on a call from a different circuit")]
    public async Task SendAsync_ShouldNotLeakTokens_WhenHandlerIsSharedAcrossCircuits()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var userAToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(15));
        using var refreshHandler = new RecordingHandler(_ => RefreshResponse(userAToken, "user-a-refresh-1"));

        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        // IHttpClientFactory reuses one handler instance across every user for its lifetime.
        var userAContext = BuildHttpContext(
            accessToken: BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-1)),
            refreshToken: "user-a-refresh-0",
            userId: "user-a",
            responseStarted: true);
        HttpContext? current = userAContext;
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns(() => current);

        var circuitServicesAccessor = new CircuitServicesAccessor();
        using var circuitA = CreateCircuit(cookieSync: CreateCookieSync(persistResult: true));
        using var circuitB = CreateCircuit(cookieSync: CreateCookieSync(persistResult: true, readResult: null));
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuitServicesAccessor: circuitServicesAccessor);
        using var client = new HttpClient(sut, disposeHandler: false);
        var ct = TestContext.Current.CancellationToken;

        // Act
        circuitServicesAccessor.Services = circuitA;
        using var userAResponse = await client.GetAsync(new Uri("https://downstream.example.com/a"), ct);

        current = null;
        circuitServicesAccessor.Services = circuitB;
        using var userBResponse = await client.GetAsync(new Uri("https://downstream.example.com/b"), ct);

        // Assert
        innerHandler.Requests.Count.ShouldBe(2);
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", userAToken));
        innerHandler.Requests[1].Headers.Authorization.ShouldBeNull();
    }

    [Fact(DisplayName = "Should adopt the browser cookie's newer tokens when another tab already redeemed this circuit's refresh token")]
    public async Task SendAsync_ShouldAdoptCookieTokens_WhenRefreshFailsBecauseAnotherTabRotated()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var refreshHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        var otherTabToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(14));
        var cookieSync = CreateCookieSync(
            persistResult: true,
            readResult: new SessionTokens("user-123", "admin@bowlneba.com", otherTabToken, "other-tab-refresh"));
        var tokenCache = SeededCache(BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-1)), refreshToken: "already-redeemed");
        using var circuit = CreateCircuit(tokenCache, cookieSync);
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", otherTabToken));
        tokenCache.RefreshToken.ShouldBe("other-tab-refresh");
    }

    [Fact(DisplayName = "Should retry the refresh with the cookie's refresh token when the cookie's access token has also expired")]
    public async Task SendAsync_ShouldRefreshWithCookieRefreshToken_WhenCookieAccessTokenIsAlsoExpired()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var newAccessToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(15));
        using var refreshHandler = new CapturingHandler(async request =>
        {
            var body = await request.Content!.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            return body.GetProperty("refreshToken").GetString() == "other-tab-refresh"
                ? RefreshResponse(newAccessToken, "refresh-after-other-tab")
                : new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        var expiredToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-1));
        var cookieSync = CreateCookieSync(
            persistResult: true,
            readResult: new SessionTokens("user-123", "admin@bowlneba.com", BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-2)), "other-tab-refresh"));
        using var circuit = CreateCircuit(SeededCache(expiredToken, refreshToken: "already-redeemed"), cookieSync);
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", newAccessToken));
        cookieSync.Verify(c => c.PersistAsync(It.Is<SessionTokens>(t => t.RefreshToken == "refresh-after-other-tab"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Should give up without another refresh when the browser cookie holds the same refresh token that just failed")]
    public async Task SendAsync_ShouldNotRetryRefresh_WhenCookieHoldsSameRefreshToken()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var refreshHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        var token = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10));
        var cookieSync = CreateCookieSync(
            persistResult: true,
            readResult: new SessionTokens("user-123", "admin@bowlneba.com", token, "refresh-abc"));
        using var circuit = CreateCircuit(SeededCache(token), cookieSync);
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        refreshHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Should still send the refreshed token when the browser cookie can't be updated")]
    public async Task SendAsync_ShouldUseRefreshedToken_WhenCookiePersistFails()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var newAccessToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(15));
        using var refreshHandler = new RecordingHandler(_ => RefreshResponse(newAccessToken, "new-refresh"));

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        var tokenCache = SeededCache(BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-1)));
        using var circuit = CreateCircuit(tokenCache, CreateCookieSync(persistResult: false));
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", newAccessToken));
        tokenCache.RefreshToken.ShouldBe("new-refresh");
    }

    [Fact(DisplayName = "Should fill an empty circuit cache from the browser cookie when there is no HttpContext")]
    public async Task SendAsync_ShouldSeedFromBrowserCookie_WhenCircuitCacheIsEmptyAndNoHttpContext()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        var cookieToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(10));
        var cookieSync = CreateCookieSync(
            persistResult: true,
            readResult: new SessionTokens("user-123", "admin@bowlneba.com", cookieToken, "refresh-abc"));
        using var circuit = CreateCircuit(cookieSync: cookieSync);
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit);
        using var client = new HttpClient(sut, disposeHandler: false);
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var first = await client.GetAsync(new Uri("https://downstream.example.com/first"), ct);
        using var second = await client.GetAsync(new Uri("https://downstream.example.com/second"), ct);

        // Assert
        innerHandler.Requests.ShouldAllBe(r => Equals(r.Headers.Authorization, new AuthenticationHeaderValue("Bearer", cookieToken)));
        cookieSync.Verify(c => c.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Should not rewrite the cookie with SignInAsync when the request's response has already started")]
    public async Task SendAsync_ShouldSkipSignIn_WhenResponseHasStarted()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var newAccessToken = BuildJwt(DateTimeOffset.UtcNow.AddMinutes(15));
        using var refreshHandler = new RecordingHandler(_ => RefreshResponse(newAccessToken, "new-refresh"));

        // Strict auth mock with no SignInAsync setup - throws if the handler tries to sign in.
        var httpContext = BuildHttpContext(
            accessToken: BuildJwt(DateTimeOffset.UtcNow.AddMinutes(-1)),
            refreshToken: "refresh-abc",
            userId: "user-123",
            responseStarted: true);
        var httpContextAccessorMock = CreateAccessor(httpContext);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", newAccessToken));
    }

    [Fact(DisplayName = "Should refresh a token on a later call once it nears expiry, even though an earlier call saw it as valid")]
    public async Task SendAsync_ShouldRefresh_WhenSameTokenNearsExpiryBetweenCalls()
    {
        // Arrange
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var newAccessToken = BuildJwt(clock.GetUtcNow().AddMinutes(15));
        using var refreshHandler = new RecordingHandler(_ => RefreshResponse(newAccessToken, "new-refresh"));

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(string.Empty)).Returns(() => new HttpClient(refreshHandler));

        var oldToken = BuildJwt(clock.GetUtcNow().AddMinutes(5));
        using var circuit = CreateCircuit(SeededCache(oldToken), CreateCookieSync(persistResult: true));
        // One handler for both calls, as IHttpClientFactory shares it for its whole lifetime.
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, circuit, timeProvider: clock);
        using var client = new HttpClient(sut, disposeHandler: false);
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var first = await client.GetAsync(new Uri("https://downstream.example.com/first"), ct);
        clock.Advance(TimeSpan.FromMinutes(5));
        using var second = await client.GetAsync(new Uri("https://downstream.example.com/second"), ct);

        // Assert
        refreshHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", oldToken));
        innerHandler.Requests[1].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", newAccessToken));
    }

    /// <summary>
    /// Builds an unsigned JWT-shaped string carrying only an "exp" claim - enough for
    /// <c>BearerTokenHandler</c>'s expiry check, which never validates the signature (that already
    /// happens server-side); it only reads the payload to decide whether to refresh proactively.
    /// </summary>
    private static string BuildJwt(DateTimeOffset expiresAt)
    {
        var header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}"u8.ToArray());
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes($"{{\"exp\":{expiresAt.ToUnixTimeSeconds()}}}"));
        return $"{header}.{payload}.";
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static BearerTokenHandler CreateHandler(
        HttpMessageHandler innerHandler,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IServiceProvider? circuit = null,
        CircuitServicesAccessor? circuitServicesAccessor = null,
        TimeProvider? timeProvider = null)
    {
        var accessor = circuitServicesAccessor ?? new CircuitServicesAccessor();
        if (circuit is not null)
            accessor.Services = circuit;

        var handler = new BearerTokenHandler(
            httpContextAccessor,
            accessor,
            httpClientFactory,
            new NebaApiConfiguration { BaseUrl = ApiBaseUrl },
            timeProvider ?? TimeProvider.System,
            NullLogger<BearerTokenHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        return handler;
    }

    /// <summary>Builds the services one Blazor circuit would hold.</summary>
    private static ServiceProvider CreateCircuit(SessionTokenCache? cache = null, Mock<ISessionCookieSync>? cookieSync = null)
        => new ServiceCollection()
            .AddSingleton(cache ?? new SessionTokenCache())
            .AddSingleton((cookieSync ?? new Mock<ISessionCookieSync>(MockBehavior.Strict)).Object)
            .BuildServiceProvider();

    private static Mock<ISessionCookieSync> CreateCookieSync(bool persistResult, SessionTokens? readResult = null)
    {
        var mock = new Mock<ISessionCookieSync>(MockBehavior.Strict);
        mock.Setup(c => c.PersistAsync(It.IsAny<SessionTokens>(), It.IsAny<CancellationToken>())).ReturnsAsync(persistResult);
        mock.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(readResult);
        return mock;
    }

    private static SessionTokenCache SeededCache(string accessToken, string refreshToken = "refresh-abc", string userId = "user-123")
    {
        var cache = new SessionTokenCache();
        cache.Set(new SessionTokens(userId, "admin@bowlneba.com", accessToken, refreshToken));
        return cache;
    }

    private static HttpResponseMessage RefreshResponse(string accessToken, string refreshToken, string userId = "user-123")
    {
        var json = JsonSerializer.Serialize(new
        {
            accessToken,
            refreshToken,
            expiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            userId,
            email = "admin@bowlneba.com",
        }, JsonOptions);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
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
        Mock<IAuthenticationService>? authServiceMock = null,
        bool responseStarted = false)
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

        if (responseStarted)
            httpContext.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var auth = authServiceMock ?? new Mock<IAuthenticationService>(MockBehavior.Strict);
        auth.Setup(s => s.AuthenticateAsync(httpContext, CookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, CookieAuthenticationDefaults.AuthenticationScheme)));

        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(auth.Object)
            .AddScoped<SessionTokenCache>()
            .BuildServiceProvider();

        return httpContext;
    }

    /// <summary>A response that has already started, as on a circuit's long-lived connection.</summary>
    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Lock _lock = new();

        // A List<T> (not thread-safe) is fine for every other test here since they only ever send
        // one request at a time; the single-flight-refresh test below sends two requests
        // concurrently, so mutation needs to be synchronized to avoid corrupting the list.
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                Requests.Add(request);
            }

            return Task.FromResult(responder(request));
        }
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responder(request);
    }

    /// <summary>
    /// A forward-only, non-seekable stream that throws if read from again once fully consumed,
    /// mirroring the one-shot semantics of a Blazor <c>IBrowserFile.OpenReadStream()</c>.
    /// </summary>
    private sealed class SingleReadStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data);
        private bool _consumed;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_inner.Position >= _inner.Length)
            {
                if (_consumed)
                    throw new InvalidOperationException("Stream already fully consumed and cannot be read again.");

                _consumed = true;
            }

            return _inner.Read(buffer, offset, count);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}