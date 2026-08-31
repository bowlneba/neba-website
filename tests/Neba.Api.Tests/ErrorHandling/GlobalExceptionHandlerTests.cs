using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

using Neba.Api.Discord;
using Neba.Api.ErrorHandling;
using Neba.TestFactory.Attributes;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.ErrorHandling;

#pragma warning disable CA2201 // Do not raise reserved exception types

[UnitTest]
[Component("Api.ErrorHandling")]
public sealed class GlobalExceptionHandlerTests
{
    private readonly Mock<IDiscordNotifier> _discordNotifier;

    public GlobalExceptionHandlerTests()
    {
        _discordNotifier = new Mock<IDiscordNotifier>(MockBehavior.Strict);
        _discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // A cache double that behaves like FusionCache's own GetOrSetAsync contract for this handler's
    // purposes: the factory runs (and its result is cached) only the first time a given key is
    // seen, so a second call for the same key is a "hit" that skips the factory - exactly the
    // debounce behavior ShouldAlertAsync relies on, without depending on real cache expiry timing.
    private static Mock<IFusionCache> CreateStatefulCache()
    {
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions());

        cache
            .Setup(c => c.GetOrSetAsync<bool>(
                It.IsAny<string>(),
                It.IsAny<Func<FusionCacheFactoryExecutionContext<bool>, CancellationToken, Task<bool>>>(),
                It.IsAny<MaybeValue<bool>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<FusionCacheFactoryExecutionContext<bool>, CancellationToken, Task<bool>>, MaybeValue<bool>, FusionCacheEntryOptions, IEnumerable<string>, CancellationToken>(
                (key, factory, _, _, _, cancel) => seenKeys.Add(key)
                    ? new ValueTask<bool>(factory(null!, cancel))
                    : new ValueTask<bool>(false));

        return cache;
    }

    // A cache double where every call is a "miss" - the factory always runs, so ShouldAlertAsync
    // always returns true. Used by tests that aren't exercising debounce behavior itself.
    private static IFusionCache CreateAlwaysAlertingCache() => CreateStatefulCache().Object;

    [Fact(DisplayName = "Should return 500 status code when exception occurs")]
    public async Task TryHandleAsync_ShouldReturn500StatusCode_WhenExceptionOccurs()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Test exception");
        var cancellationToken = CancellationToken.None;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.Is<ProblemDetailsContext>(ctx =>
                ctx.HttpContext == httpContext &&
                ctx.Exception == exception &&
                ctx.ProblemDetails.Status == StatusCodes.Status500InternalServerError &&
                ctx.ProblemDetails.Detail == "An unhandled exception occurred while processing the request.")))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        result.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemDetailsServiceMock.Verify(
            s => s.TryWriteAsync(It.Is<ProblemDetailsContext>(ctx =>
                ctx.HttpContext == httpContext &&
                ctx.Exception == exception)),
            Times.Once);
    }

    [Fact(DisplayName = "Should write problem details with correct properties")]
    public async Task TryHandleAsync_ShouldWriteProblemDetails_WithCorrectProperties()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/test-path";
        var exception = new Exception("Test error");
        var cancellationToken = CancellationToken.None;

        ProblemDetailsContext? capturedContext = null;
        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(ctx => capturedContext = ctx)
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.HttpContext.ShouldBe(httpContext);
        capturedContext.Exception.ShouldBe(exception);
        capturedContext.ProblemDetails.ShouldNotBeNull();
        capturedContext.ProblemDetails.Status.ShouldBe(StatusCodes.Status500InternalServerError);
        capturedContext.ProblemDetails.Detail.ShouldBe("An unhandled exception occurred while processing the request.");
    }

    [Fact(DisplayName = "Should return result from problem details service")]
    public async Task TryHandleAsync_ShouldReturnServiceResult_WhenServiceReturnsTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Test");
        var cancellationToken = CancellationToken.None;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should return result from problem details service when false")]
    public async Task TryHandleAsync_ShouldReturnServiceResult_WhenServiceReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Test");
        var cancellationToken = CancellationToken.None;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(false);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory(DisplayName = "Should handle different exception types")]
    [InlineData(typeof(InvalidOperationException), "Invalid operation", TestDisplayName = "InvalidOperationException")]
    [InlineData(typeof(ArgumentException), "Bad argument", TestDisplayName = "ArgumentException")]
    [InlineData(typeof(NullReferenceException), "Null reference", TestDisplayName = "NullReferenceException")]
    public async Task TryHandleAsync_ShouldHandleDifferentExceptionTypes_WithCorrectDetails(
        Type exceptionType,
        string message)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;
        var cancellationToken = CancellationToken.None;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.Is<ProblemDetailsContext>(ctx =>
                ctx.Exception == exception &&
                ctx.ProblemDetails.Status == StatusCodes.Status500InternalServerError)))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        result.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact(DisplayName = "Should set status code before writing problem details")]
    public async Task TryHandleAsync_ShouldSetStatusCode_BeforeWritingProblemDetails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Test");
        var cancellationToken = CancellationToken.None;
        var statusCodeWhenWriteCalled = 0;

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback(() => statusCodeWhenWriteCalled = httpContext.Response.StatusCode)
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        statusCodeWhenWriteCalled.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact(DisplayName = "Should log error when exception occurs")]
    public async Task TryHandleAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Test exception");
        var logger = new FakeLogger<GlobalExceptionHandler>();

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(problemDetailsServiceMock.Object, _discordNotifier.Object, CreateAlwaysAlertingCache(), logger);

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldHaveSingleItem();
        logs[0].Level.ShouldBe(LogLevel.Error);
    }

    [Fact(DisplayName = "Should post critical Discord alert with exception details when exception occurs")]
    public async Task TryHandleAsync_ShouldPostCriticalDiscordAlert_WhenExceptionOccurs()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/test-path";
        var exception = new InvalidOperationException("Test exception");
        var cancellationToken = CancellationToken.None;

        DiscordAlert? capturedAlert = null;
        _discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), cancellationToken))
            .Callback<DiscordAlert, CancellationToken>((alert, _) => capturedAlert = alert)
            .Returns(Task.CompletedTask);

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        await handler.TryHandleAsync(httpContext, exception, cancellationToken);

        // Assert
        capturedAlert.ShouldNotBeNull();
        capturedAlert.Severity.ShouldBe(DiscordAlertSeverity.Critical);
        capturedAlert.Title.ShouldBe("Unhandled exception occurred");
        capturedAlert.Body.ShouldBe(exception.Message);
        capturedAlert.Metadata.ShouldNotBeNull();
        capturedAlert.Metadata["ExceptionType"].ShouldBe(exception.GetType().FullName);
        capturedAlert.Metadata["RequestPath"].ShouldBe("/test-path");
        capturedAlert.Metadata.ShouldNotContainKey("StackTrace");
        _discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), cancellationToken), Times.Once);
    }

    [Fact(DisplayName = "Should mask an email address embedded in the exception message before posting to Discord")]
    public async Task TryHandleAsync_ShouldMaskEmbeddedEmailAddress_InDiscordAlertBody()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Bowler with email jdoe@example.com already exists");

        DiscordAlert? capturedAlert = null;
        _discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback<DiscordAlert, CancellationToken>((alert, _) => capturedAlert = alert)
            .Returns(Task.CompletedTask);

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedAlert.ShouldNotBeNull();
        capturedAlert.Body.ShouldNotContain("jdoe@example.com");
        capturedAlert.Body.ShouldBe("Bowler with email j*************** already exists");
    }

    [Fact(DisplayName = "Should notify Discord before writing problem details")]
    public async Task TryHandleAsync_ShouldNotifyDiscord_BeforeWritingProblemDetails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new Exception("Test");
        var discordNotified = false;
        var discordNotifiedWhenWriteCalled = false;

        _discordNotifier
            .Setup(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()))
            .Callback(() => discordNotified = true)
            .Returns(Task.CompletedTask);

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback(() => discordNotifiedWhenWriteCalled = discordNotified)
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        discordNotifiedWhenWriteCalled.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should still write the 500 response when the ambient cancellation token is already canceled")]
    public async Task TryHandleAsync_ShouldStillWrite500Response_WhenAmbientTokenIsCanceled()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Test exception");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            CreateAlwaysAlertingCache(),
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        var result = await handler.TryHandleAsync(httpContext, exception, cts.Token);

        // Assert
        result.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        _discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), CancellationToken.None), Times.Once);
    }

    [Fact(DisplayName = "Should debounce the entry for 5 minutes")]
    public async Task TryHandleAsync_ShouldDebounceEntry_ForFiveMinutes()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("Test exception");

        FusionCacheEntryOptions? capturedOptions = null;
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);
        cache.SetupGet(c => c.DefaultEntryOptions).Returns(new FusionCacheEntryOptions());
        cache
            .Setup(c => c.GetOrSetAsync<bool>(
                It.IsAny<string>(),
                It.IsAny<Func<FusionCacheFactoryExecutionContext<bool>, CancellationToken, Task<bool>>>(),
                It.IsAny<MaybeValue<bool>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<FusionCacheFactoryExecutionContext<bool>, CancellationToken, Task<bool>>, MaybeValue<bool>, FusionCacheEntryOptions, IEnumerable<string>, CancellationToken>(
                (_, _, _, options, _, _) => capturedOptions = options)
            .Returns<string, Func<FusionCacheFactoryExecutionContext<bool>, CancellationToken, Task<bool>>, MaybeValue<bool>, FusionCacheEntryOptions, IEnumerable<string>, CancellationToken>(
                (_, factory, _, _, _, cancel) => new ValueTask<bool>(factory(null!, cancel)));

        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            cache.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        capturedOptions.ShouldNotBeNull();
        capturedOptions.Duration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact(DisplayName = "Should not repost the same exception type and path within the debounce window")]
    public async Task TryHandleAsync_ShouldNotRepostDiscordAlert_WhenSameExceptionAndPathWithinDebounceWindow()
    {
        // Arrange
        var cache = CreateStatefulCache();
        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            cache.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        var firstContext = new DefaultHttpContext();
        firstContext.Request.Path = "/test-path";
        var secondContext = new DefaultHttpContext();
        secondContext.Request.Path = "/test-path";

        // Act
        await handler.TryHandleAsync(firstContext, new InvalidOperationException("First"), CancellationToken.None);
        await handler.TryHandleAsync(secondContext, new InvalidOperationException("Second"), CancellationToken.None);

        // Assert
        _discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Should post a Discord alert for a different request path even within the debounce window")]
    public async Task TryHandleAsync_ShouldPostDiscordAlert_WhenDifferentRequestPathWithinDebounceWindow()
    {
        // Arrange
        var cache = CreateStatefulCache();
        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            cache.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        var firstContext = new DefaultHttpContext();
        firstContext.Request.Path = "/first-path";
        var secondContext = new DefaultHttpContext();
        secondContext.Request.Path = "/second-path";

        // Act
        await handler.TryHandleAsync(firstContext, new InvalidOperationException("First"), CancellationToken.None);
        await handler.TryHandleAsync(secondContext, new InvalidOperationException("Second"), CancellationToken.None);

        // Assert
        _discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "Should post a Discord alert for a different exception type on the same path even within the debounce window")]
    public async Task TryHandleAsync_ShouldPostDiscordAlert_WhenDifferentExceptionTypeWithinDebounceWindow()
    {
        // Arrange
        var cache = CreateStatefulCache();
        var problemDetailsServiceMock = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .ReturnsAsync(true);

        var handler = new GlobalExceptionHandler(
            problemDetailsServiceMock.Object,
            _discordNotifier.Object,
            cache.Object,
            NullLogger<GlobalExceptionHandler>.Instance);

        var firstContext = new DefaultHttpContext();
        firstContext.Request.Path = "/test-path";
        var secondContext = new DefaultHttpContext();
        secondContext.Request.Path = "/test-path";

        // Act
        await handler.TryHandleAsync(firstContext, new InvalidOperationException("First"), CancellationToken.None);
        await handler.TryHandleAsync(secondContext, new ArgumentException("Second"), CancellationToken.None);

        // Assert
        _discordNotifier.Verify(n => n.NotifyAsync(It.IsAny<DiscordAlert>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}