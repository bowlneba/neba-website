using System.Text.Json;

using ErrorOr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Application.Caching;
using Neba.Application.Messaging;
using Neba.Api.Caching;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Caching;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Tests.Caching;

[UnitTest]
[Component("Infrastructure.Caching")]
public sealed class CachedQueryHandlerDecoratorTests
{
    private static readonly IServiceProvider ServiceProvider = new ServiceCollection().BuildServiceProvider();

    public sealed record TestResponse(string Value);

    public sealed record PlainQuery(
        CacheDescriptor Cache,
        TimeSpan Expiry) : ICachedQuery<TestResponse>;

    public sealed record ErrorOrQuery(
        CacheDescriptor Cache,
        TimeSpan Expiry) : ICachedQuery<ErrorOr<TestResponse>>;

    // ─── Plain response ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Plain response: cache hit returns cached value without calling inner handler")]
    public async Task HandleAsync_PlainResponse_CacheHit_ReturnsCachedValue()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new PlainQuery(CacheDescriptorFactory.Create(key: "key:plain:hit"), TimeSpan.FromMinutes(5));
        var cachedValue = new TestResponse("cached");

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.GetOrSetAsync<TestResponse>(
                query.Cache.Key,
                It.IsAny<Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>>(),
                It.IsAny<MaybeValue<TestResponse>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedValue);

        var result = await CreatePlainDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.ShouldBe(cachedValue);
        cache.Verify(
            c => c.GetOrSetAsync<TestResponse>(
                query.Cache.Key,
                It.IsAny<Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>>(),
                It.IsAny<MaybeValue<TestResponse>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact(DisplayName = "Plain response: cache miss calls inner handler and returns result")]
    public async Task HandleAsync_PlainResponse_CacheMiss_CallsHandlerAndReturnsResult()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new PlainQuery(CacheDescriptorFactory.Create(key: "key:plain:miss"), TimeSpan.FromMinutes(5));
        var handlerResult = new TestResponse("fresh");

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.GetOrSetAsync<TestResponse>(
                query.Cache.Key,
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

    [Fact(DisplayName = "Plain response: constructor succeeds without calling GetInnerType for non-ErrorOr type")]
    public void Constructor_PlainResponseType_DoesNotThrow()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        // Mutation forces GetInnerType(typeof(TestResponse)) which throws ArgumentException
        // because TestResponse is not ErrorOr<T>
        Should.NotThrow(() =>
            new CachedQueryHandlerDecorator<PlainQuery, TestResponse>(
                innerHandler.Object,
                cache.Object,
                NullLogger<CachedQueryHandlerDecorator<PlainQuery, TestResponse>>.Instance,
                ServiceProvider));
    }

    [Fact(DisplayName = "Plain response: deserialization failure falls back to handler and refreshes cache")]
    public async Task HandleAsync_PlainResponse_DeserializationFailure_FallsBackToHandlerAndRefreshesCache()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var descriptor = CacheDescriptorFactory.Create(
            key: "key:plain:deserialization",
            tags: ["tag:a", "tag:b"]);
        var query = new PlainQuery(descriptor, TimeSpan.FromMinutes(5));
        var handlerResult = new TestResponse("fresh-after-failure");

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.GetOrSetAsync<TestResponse>(
                query.Cache.Key,
                It.IsAny<Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>>(),
                It.IsAny<MaybeValue<TestResponse>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Throws(new NotSupportedException("Deserialization failed for cached entry"));

        innerHandler
            .Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        cache
            .Setup(c => c.SetAsync(
                query.Cache.Key,
                handlerResult,
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(descriptor.Tags)),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var result = await CreatePlainDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.ShouldBe(handlerResult);
        cache.Verify(
            c => c.SetAsync(
                query.Cache.Key,
                handlerResult,
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(descriptor.Tags)),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    // ─── ErrorOr response ─────────────────────────────────────────────────────

    [Fact(DisplayName = "ErrorOr response: L1 cache hit rewraps typed object and returns it")]
    public async Task HandleAsync_ErrorOrResponse_CacheHitL1_ReturnsWrappedValue()
    {
        var innerHandler = new Mock<IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new ErrorOrQuery(CacheDescriptorFactory.Create(key: "key:error-or"), TimeSpan.FromMinutes(5));
        var innerValue = new TestResponse("from-l1");

        cache
            .Setup(c => c.TryGetAsync<object>(
                query.Cache.Key,
                It.IsAny<FusionCacheEntryOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaybeValue<object>)innerValue);

        var result = await CreateErrorOrDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(innerValue);
        innerHandler.Verify(
            h => h.HandleAsync(It.IsAny<ErrorOrQuery>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact(DisplayName = "ErrorOr response: L2 cache hit deserializes JsonElement and returns wrapped value")]
    public async Task HandleAsync_ErrorOrResponse_CacheHitL2_DeserializesAndReturnsWrappedValue()
    {
        var innerHandler = new Mock<IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new ErrorOrQuery(CacheDescriptorFactory.Create(key: "key:error-or"), TimeSpan.FromMinutes(5));
        var innerValue = new TestResponse("from-l2");
        var jsonElement = JsonDocument.Parse(JsonSerializer.Serialize(innerValue)).RootElement;

        cache
            .Setup(c => c.TryGetAsync<object>(
                query.Cache.Key,
                It.IsAny<FusionCacheEntryOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaybeValue<object>)jsonElement);

        var result = await CreateErrorOrDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(innerValue);
        innerHandler.Verify(
            h => h.HandleAsync(It.IsAny<ErrorOrQuery>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact(DisplayName = "ErrorOr response: cache miss with error result returns error without caching")]
    public async Task HandleAsync_ErrorOrResponse_CacheMiss_ErrorResult_ReturnsErrorWithoutCaching()
    {
        var innerHandler = new Mock<IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new ErrorOrQuery(CacheDescriptorFactory.Create(key: "key:error-or"), TimeSpan.FromMinutes(5));
        ErrorOr<TestResponse> errorResult = Error.NotFound("Test.NotFound", "not found");

        cache
            .Setup(c => c.TryGetAsync<object>(
                query.Cache.Key,
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

        var descriptor = CacheDescriptorFactory.Create(
            key: "neba:document:bylaws:content",
            tags: ["neba:documents", "neba:document:bylaws"]);
        var query = new ErrorOrQuery(descriptor, TimeSpan.FromMinutes(5));
        var innerValue = new TestResponse("fresh");
        ErrorOr<TestResponse> successResult = innerValue;

        cache
            .Setup(c => c.TryGetAsync<object>(
                query.Cache.Key,
                It.IsAny<FusionCacheEntryOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(MaybeValue<object>));

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.SetAsync(
                query.Cache.Key,
                It.IsAny<object>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(descriptor.Tags)),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        innerHandler
            .Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        var result = await CreateErrorOrDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(innerValue);
        cache.Verify(
            c => c.TryGetAsync<object>(
                query.Cache.Key,
                It.IsAny<FusionCacheEntryOptions?>(),
                It.IsAny<CancellationToken>()),
            Times.Once());
        cache.Verify(
            c => c.SetAsync(
                query.Cache.Key,
                It.Is<object>(v => v.Equals(innerValue)),
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(descriptor.Tags)),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    // ─── IsCacheDeserializationException ─────────────────────────────────────

    [Fact(DisplayName = "Plain response: JsonException falls back to handler and refreshes cache")]
    public async Task HandleAsync_PlainResponse_JsonException_FallsBackToHandlerAndRefreshesCache()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var descriptor = CacheDescriptorFactory.Create(key: "key:plain:json-exception", tags: ["tag:json"]);
        var query = new PlainQuery(descriptor, TimeSpan.FromMinutes(5));
        var handlerResult = new TestResponse("fresh-after-json-failure");

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.GetOrSetAsync<TestResponse>(
                query.Cache.Key,
                It.IsAny<Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>>(),
                It.IsAny<MaybeValue<TestResponse>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Throws(new JsonException("Deserialization failed"));

        innerHandler
            .Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        cache
            .Setup(c => c.SetAsync(
                query.Cache.Key,
                handlerResult,
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(descriptor.Tags)),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var result = await CreatePlainDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.ShouldBe(handlerResult);
        cache.Verify(
            c => c.SetAsync(
                query.Cache.Key,
                handlerResult,
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(descriptor.Tags)),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact(DisplayName = "Plain response: nested JsonException in InnerException chain falls back to handler")]
    public async Task HandleAsync_PlainResponse_NestedJsonException_FallsBackToHandlerAndRefreshesCache()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var descriptor = CacheDescriptorFactory.Create(key: "key:plain:nested-json", tags: ["tag:nested"]);
        var query = new PlainQuery(descriptor, TimeSpan.FromMinutes(5));
        var handlerResult = new TestResponse("fresh-after-nested-failure");

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.GetOrSetAsync<TestResponse>(
                query.Cache.Key,
                It.IsAny<Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>>(),
                It.IsAny<MaybeValue<TestResponse>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Outer exception", new JsonException("Root deserialization cause")));

        innerHandler
            .Setup(h => h.HandleAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        cache
            .Setup(c => c.SetAsync(
                query.Cache.Key,
                handlerResult,
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(descriptor.Tags)),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var result = await CreatePlainDecorator(innerHandler.Object, cache.Object)
            .HandleAsync(query, CancellationToken.None);

        result.ShouldBe(handlerResult);
        cache.Verify(
            c => c.SetAsync(
                query.Cache.Key,
                handlerResult,
                It.IsAny<FusionCacheEntryOptions>(),
                It.Is<IEnumerable<string>>(t => t.SequenceEqual(descriptor.Tags)),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact(DisplayName = "Plain response: NotSupportedException without 'deserialization' in message propagates without fallback")]
    public async Task HandleAsync_PlainResponse_NotSupportedExceptionWithUnrelatedMessage_Propagates()
    {
        var innerHandler = new Mock<IQueryHandler<PlainQuery, TestResponse>>(MockBehavior.Strict);
        var cache = new Mock<IFusionCache>(MockBehavior.Strict);

        var query = new PlainQuery(CacheDescriptorFactory.Create(key: "key:plain:nse-unrelated"), TimeSpan.FromMinutes(5));

        cache
            .SetupGet(c => c.DefaultEntryOptions)
            .Returns(new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) });

        cache
            .Setup(c => c.GetOrSetAsync<TestResponse>(
                query.Cache.Key,
                It.IsAny<Func<FusionCacheFactoryExecutionContext<TestResponse>, CancellationToken, Task<TestResponse>>>(),
                It.IsAny<MaybeValue<TestResponse>>(),
                It.IsAny<FusionCacheEntryOptions>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Throws(new NotSupportedException("Cache serializer is not configured"));

        await Should.ThrowAsync<NotSupportedException>(() =>
            CreatePlainDecorator(innerHandler.Object, cache.Object)
                .HandleAsync(query, CancellationToken.None));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CachedQueryHandlerDecorator<PlainQuery, TestResponse> CreatePlainDecorator(
        IQueryHandler<PlainQuery, TestResponse> innerHandler,
        IFusionCache cache) =>
        new(innerHandler, cache,
            NullLogger<CachedQueryHandlerDecorator<PlainQuery, TestResponse>>.Instance,
            ServiceProvider);

    private static CachedQueryHandlerDecorator<ErrorOrQuery, ErrorOr<TestResponse>> CreateErrorOrDecorator(
        IQueryHandler<ErrorOrQuery, ErrorOr<TestResponse>> innerHandler,
        IFusionCache cache) =>
        new(innerHandler, cache,
            NullLogger<CachedQueryHandlerDecorator<ErrorOrQuery, ErrorOr<TestResponse>>>.Instance,
            ServiceProvider);
}