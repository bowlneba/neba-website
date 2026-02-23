using System.Text.Json;

using ErrorOr;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Neba.Application.Messaging;
using Neba.Infrastructure.Caching;
using Neba.TestFactory.Attributes;

using Shouldly;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Infrastructure.Tests.Caching;

[UnitTest]
[Component("Infrastructure.Caching")]
public sealed class CachedQueryHandlerDecoratorTests
{
    private sealed record TestResponse(string Value);

    private sealed record PlainQuery(
        string CacheKey,
        TimeSpan Expiry,
        IReadOnlyCollection<string> Tags) : ICachedQuery<TestResponse>;

    private sealed record ErrorOrQuery(
        string CacheKey,
        TimeSpan Expiry,
        IReadOnlyCollection<string> Tags) : ICachedQuery<ErrorOr<TestResponse>>;

    // ─── Plain response ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Plain response: cache hit returns cached value without calling inner handler")]
    public async Task HandleAsync_PlainResponse_CacheHit_ReturnsCachedValue()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new PlainQuery("key:plain:hit", TimeSpan.FromMinutes(5), []);
        var cachedValue = new TestResponse("cached");

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.GetOrSetAsync<TestResponse>(
                query.CacheKey,
                It.IsAny<Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>>(),
                It.IsAny<MaybeValue<TestResponse>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedValue);

        var result = await CreatePlainDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.ShouldBe(cachedValue);
    }

    [Fact(DisplayName = "Plain response: cache miss calls inner handler and returns result")]
    public async Task HandleAsync_PlainResponse_CacheMiss_CallsHandlerAndReturnsResult()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new PlainQuery("key:plain:miss", TimeSpan.FromMinutes(5), []);
        var handlerResult = new TestResponse("fresh");

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.GetOrSetAsync<TestResponse>(
                query.CacheKey,
                It.IsAny<Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>>(),
                It.IsAny<MaybeValue<TestResponse>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>, MaybeValue<TestResponse>, FusionCacheEntryOptions, IEnumerable<string>, CancellationToken>(
                (_, factory, _, _, _, cancel) => new ValueTask<TestResponse>(factory(null!, cancel)));

        innerHandler
            .Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        var result = await CreatePlainDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.ShouldBe(handlerResult);
    }

    // ─── ErrorOr response ─────────────────────────────────────────────────────

    [Fact(DisplayName = "ErrorOr response: L1 cache hit rewraps typed object and returns it")]
    public async Task HandleAsync_ErrorOrResponse_CacheHitL1_ReturnsWrappedValue()
    {
        var innerHandler = new Mock<IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new ErrorOrQuery("key:error-or", TimeSpan.FromMinutes(5), []);
        var innerValue = new TestResponse("from-l1");

        cache
            .Setup(c => c.TryGetAsync<object>(
                query.CacheKey,
                It.IsAny<FusionCacheEntryOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaybeValue<object>)innerValue);

        var result = await CreateErrorOrDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(innerValue);
    }

    [Fact(DisplayName = "ErrorOr response: L2 cache hit deserializes JsonElement and returns wrapped value")]
    public async Task HandleAsync_ErrorOrResponse_CacheHitL2_DeserializesAndReturnsWrappedValue()
    {
        var innerHandler = new Mock<IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new ErrorOrQuery("key:error-or", TimeSpan.FromMinutes(5), []);
        var innerValue = new TestResponse("from-l2");
        var jsonElement = JsonDocument.Parse(JsonSerializer.Serialize(innerValue)).RootElement;

        cache
            .Setup(c => c.TryGetAsync<object>(
                query.CacheKey,
                It.IsAny<FusionCacheEntryOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaybeValue<object>)jsonElement);

        var result = await CreateErrorOrDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(innerValue);
    }

    [Fact(DisplayName = "ErrorOr response: cache miss with error result returns error without caching")]
    public async Task HandleAsync_ErrorOrResponse_CacheMiss_ErrorResult_ReturnsErrorWithoutCaching()
    {
        var innerHandler = new Mock<IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new ErrorOrQuery("key:error-or", TimeSpan.FromMinutes(5), []);
        ErrorOr<TestResponse> errorResult = Error.NotFound("Test.NotFound", "not found");

        cache
            .Setup(c => c.TryGetAsync<object>(
                query.CacheKey,
                It.IsAny<FusionCacheEntryOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(MaybeValue<object>));

        innerHandler
            .Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        var result = await CreateErrorOrDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact(DisplayName = "ErrorOr response: cache miss with success result caches inner value with tags and returns response")]
    public async Task HandleAsync_ErrorOrResponse_CacheMiss_SuccessResult_CachesWithTagsAndReturnsResponse()
    {
        var innerHandler = new Mock<IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var tags = new[] { "website", "website:docs" };
        var query = new ErrorOrQuery("key:error-or", TimeSpan.FromMinutes(5), tags);
        var innerValue = new TestResponse("fresh");
        ErrorOr<TestResponse> successResult = innerValue;

        cache
            .Setup(c => c.TryGetAsync<object>(
                query.CacheKey,
                It.IsAny<FusionCacheEntryOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(MaybeValue<object>));

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.SetAsync(
                query.CacheKey,
                It.IsAny<object>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(tags)),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        innerHandler
            .Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var result = await CreateErrorOrDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(innerValue);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CachedQueryHandlerDecorator<PlainQuery, TestResponse> CreatePlainDecorator(
        IQueryHandler<PlainQuery, TestResponse> innerHandler,
        IFusionCache cache) =>
        new(innerHandler, cache,
            NullLogger<CachedQueryHandlerDecorator<PlainQuery, TestResponse>>.Instance);

    private static CachedQueryHandlerDecorator<ErrorOrQuery, ErrorOr<TestResponse>> CreateErrorOrDecorator(
        IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>> innerHandler,
        IFusionCache cache) =>
        new(innerHandler, cache,
            NullLogger<CachedQueryHandlerDecorator<ErrorOrQuery, ErrorOr<TestResponse>>>.Instance);
}
