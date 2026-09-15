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

    [Fact(DisplayName = "Should attach the cached access token as a bearer header when there is no HttpContext")]
    public async Task SendAsync_ShouldAttachCachedBearerHeader_WhenNoHttpContext()
    {
        // Arrange
        using var innerHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpContextAccessorMock.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        var factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var tokenCache = new CircuitTokenCache { AccessToken = "circuit-token" };
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, tokenCache);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "circuit-token"));
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
        var tokenCache = new CircuitTokenCache { AccessToken = expiredToken, RefreshToken = "refresh-abc", UserId = "user-123" };
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, tokenCache);
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
        var tokenCache = new CircuitTokenCache { AccessToken = validToken, RefreshToken = "refresh-abc", UserId = "user-123" };
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, tokenCache);
        using var client = new HttpClient(sut, disposeHandler: false);

        // Act
        using var response = await client.GetAsync(new Uri("https://downstream.example.com/resource"), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.Requests.ShouldHaveSingleItem();
        innerHandler.Requests[0].Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", validToken));
    }

    [Fact(DisplayName = "Should silently refresh and retry using the cached refresh token when there is no HttpContext")]
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
        var tokenCache = new CircuitTokenCache { AccessToken = oldToken, RefreshToken = "refresh-abc", UserId = "user-123" };
        using var sut = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, tokenCache);
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
        // shared CircuitTokenCache.RefreshLock while the first call's refresh is still in flight -
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
        // separate transient BearerTokenHandler instances but the same scoped CircuitTokenCache.
        var tokenCache = new CircuitTokenCache { AccessToken = expiredToken, RefreshToken = "refresh-abc", UserId = "user-123" };

        using var sutA = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, tokenCache);
        using var sutB = CreateHandler(innerHandler, httpContextAccessorMock.Object, factoryMock.Object, tokenCache);
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
        CircuitTokenCache? tokenCache = null)
    {
        var handler = new BearerTokenHandler(
            httpContextAccessor,
            tokenCache ?? new CircuitTokenCache(),
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
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => await responder(request);
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