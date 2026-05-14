using System.Text.Json;

using ErrorOr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neba.Application.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Infrastructure.Caching;

internal sealed class CachedQueryHandlerDecorator<TQuery, TResponse>
    : IQueryHandler<TQuery, TResponse>
    where TQuery : ICachedQuery<TResponse>
{
    private readonly IQueryHandler<TQuery, TResponse> _innerHandler;
    private readonly IFusionCache _cache;
    private readonly ILogger<CachedQueryHandlerDecorator<TQuery, TResponse>> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private readonly bool _isErrorOrResponse;
    private readonly Type? _innerType;

    public CachedQueryHandlerDecorator(
        IQueryHandler<TQuery, TResponse> innerHandler,
        IFusionCache cache,
        ILogger<CachedQueryHandlerDecorator<TQuery, TResponse>> logger,
        IServiceProvider serviceProvider)
    {
        _innerHandler = innerHandler;
        _cache = cache;
        _logger = logger;
        _jsonOptions = serviceProvider.GetKeyedService<JsonSerializerOptions>(HybridCacheSerializerOptionsKey.Key)
            ?? JsonSerializerOptions.Default;

        _isErrorOrResponse = ErrorOrCacheHelper.IsErrorOrType(typeof(TResponse));
        _innerType = _isErrorOrResponse ? ErrorOrCacheHelper.GetInnerType(typeof(TResponse)) : null;
    }

    public Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken)
        => _isErrorOrResponse
            ? HandleErrorOrResponseAsync(query, cancellationToken)
            : HandleResponseAsync(query, cancellationToken);

    private async Task<TResponse> HandleResponseAsync(ICachedQuery<TResponse> query, CancellationToken cancellationToken)
    {
        var cacheKey = query.Cache.Key;

        var options = _cache.DefaultEntryOptions.Duplicate();
        options.Duration = query.Expiry;

        try
        {
            return await _cache.GetOrSetAsync<TResponse>(
                cacheKey,
                async (_, cancel) =>
                {
                    _logger.LogCacheMiss(cacheKey);
                    return await _innerHandler.HandleAsync((TQuery)query, cancel);
                },
                failSafeDefaultValue: default,
                options: options,
                tags: query.Cache.Tags,
                token: cancellationToken);
        }
        catch (Exception ex) when (IsCacheDeserializationException(ex))
        {
            _logger.LogCacheDeserializationFallback(cacheKey, ex);

            var fresh = await _innerHandler.HandleAsync((TQuery)query, cancellationToken);
            await _cache.SetAsync(cacheKey, fresh!, options, tags: query.Cache.Tags, token: cancellationToken);

            return fresh;
        }
    }

    private static bool IsCacheDeserializationException(Exception exception)
    {
        Exception? current = exception;

        while (current is not null)
        {
            if (current is JsonException)
            {
                return true;
            }

            if (current is NotSupportedException &&
                current.Message.Contains("deserialization", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private async Task<TResponse> HandleErrorOrResponseAsync(ICachedQuery<TResponse> query, CancellationToken cancellationToken)
    {
        var cacheKey = query.Cache.Key;

        var cached = await _cache.TryGetAsync<object>(cacheKey, token: cancellationToken);

        if (cached.HasValue)
        {
            try
            {
                _logger.LogCacheHit(cacheKey);

                // L1 hit: cached.Value is the original typed object
                // L2 hit: cached.Value is the unwrapped inner value deserialized via cache options
                var value = cached.Value is JsonElement jsonElement
                    ? JsonSerializer.Deserialize(jsonElement.GetRawText(), _innerType!, _jsonOptions)
                    : cached.Value;

                _logger.LogRewrappingErrorOr(cacheKey);

                return (TResponse)ErrorOrCacheHelper.WrapValue(_innerType!, value!);
            }
            catch (Exception ex) when (IsCacheDeserializationException(ex))
            {
                _logger.LogCacheDeserializationFallback(cacheKey, ex);
                // fall through to execute inner handler and refresh cache
            }
        }

        _logger.LogCacheMiss(cacheKey);

        var response = await _innerHandler.HandleAsync((TQuery)query, cancellationToken);

        // Box first: compiler rejects direct cast from unconstrained generic to interface.
        // Safe here because _isErrorOrResponse guarantees TResponse is ErrorOr<T> : IErrorOr.
        if (ErrorOrCacheHelper.IsError((IErrorOr)(object)response!))
        {
            _logger.LogErrorResultNotCached(cacheKey);
            return response;
        }

        _logger.LogUnwrappingErrorOr(cacheKey);

        var innerValue = ErrorOrCacheHelper.GetValue(response!);

        var options = _cache.DefaultEntryOptions.Duplicate();
        options.Duration = query.Expiry;
        await _cache.SetAsync(cacheKey, innerValue!, options, tags: query.Cache.Tags, token: cancellationToken);

        return response;
    }
}

internal static partial class CachedQueryHandlerDecoratorLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Cache hit for key '{CacheKey}'")]
    public static partial void LogCacheHit(
        this ILogger logger,
        string cacheKey);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cache miss for key '{CacheKey}', executing query handler")]
    public static partial void LogCacheMiss(
        this ILogger logger,
        string cacheKey);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Skipping cache for key '{CacheKey}' due to error result")]
    public static partial void LogErrorResultNotCached(
        this ILogger logger,
        string cacheKey);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Unwrapping ErrorOr value for cache storage: key '{CacheKey}'")]
    public static partial void LogUnwrappingErrorOr(
        this ILogger logger,
        string cacheKey);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Rewrapping cached value into ErrorOr: key '{CacheKey}'")]
    public static partial void LogRewrappingErrorOr(
        this ILogger logger,
        string cacheKey);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Cache deserialization failed for key '{CacheKey}', refreshing entry from source")]
    public static partial void LogCacheDeserializationFallback(
        this ILogger logger,
        string cacheKey,
        Exception exception);
}