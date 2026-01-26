using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Neba.Application.Clock;
using Neba.Application.Messaging;

namespace Neba.Infrastructure.Telemetry.Tracing;

internal sealed class TracedQueryHandlerDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> innerHandler,
    IStopwatchProvider stopwatchProvider,
    ILogger<TracedQueryHandlerDecorator<TQuery, TResponse>> logger)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private static readonly ActivitySource ActivitySource
        = new("Neba.Handlers");

    private readonly IQueryHandler<TQuery, TResponse> _innerHandler = innerHandler;
    private readonly IStopwatchProvider _stopwatchProvider = stopwatchProvider;
    private readonly ILogger<TracedQueryHandlerDecorator<TQuery, TResponse>> _logger = logger;
    private readonly string _queryType = typeof(TQuery).Name;
    private readonly string _responseType = typeof(TResponse).Name;
    private readonly bool _isCached = typeof(ICachedQuery<TResponse>).IsAssignableFrom(typeof(TQuery));

    public async Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        using Activity? activity = ActivitySource.StartActivity($"query.{_queryType}", ActivityKind.Server);

        activity?.SetCodeAttributes(_queryType, "Neba.Handlers");
        activity?.SetTag("handler.type", "query");
        activity?.SetTag("response.type", _responseType);
        activity?.SetTag("query.cached", _isCached);

        if (_isCached && query is ICachedQuery<TResponse> cachedQuery)
        {
            activity?.SetTag("query.cache.key", cachedQuery.CacheKey);
            activity?.SetTag("query.cache.expiry", cachedQuery.Expiry.TotalSeconds);
        }

        var startTimestamp = _stopwatchProvider.GetTimestamp();

        try
        {
            var result = await _innerHandler.HandleAsync(query, cancellationToken);

            double durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

            activity?.SetTag("query.duration_ms", durationMs);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return result;
        }
        catch (Exception ex)
        {
            double durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

            activity?.SetTag("query.duration_ms", durationMs);
            activity?.SetExceptionTags(ex);

            _logger.LogQueryExecutionFailed(_queryType, durationMs, ex);

            throw;
        }
    }
}