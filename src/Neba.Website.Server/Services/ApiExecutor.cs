using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using ErrorOr;

using Neba.Website.Server.Clock;
using Neba.Website.Server.Telemetry.Metrics;

using Refit;

namespace Neba.Website.Server.Services;

#pragma warning disable CA1031 // Do not catch general exception types

internal sealed class ApiExecutor(
    IStopwatchProvider stopwatchProvider,
    ILogger<ApiExecutor> logger)
{
    private static readonly ActivitySource ActivitySource = new("Neba.Website.Server");

    public async Task<ErrorOr<TResponse>> ExecuteAsync<TResponse>(
        string apiName,
        string operationName,
        Func<CancellationToken, Task<IApiResponse<TResponse>>> apiCall,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(
            $"{apiName}.{operationName}",
            ActivityKind.Client
        );

        activity?.SetTag("code.function", operationName);
        activity?.SetTag("code.namespace", apiName);
        activity?.SetTag(ApiMetricTagNames.ApiName, apiName);
        activity?.SetTag(ApiMetricTagNames.OperationName, operationName);

        ApiMetrics.RecordApiCall(apiName, operationName);
        var startTimestamp = stopwatchProvider.GetTimestamp();

        try
        {
            var response = await apiCall(cancellationToken);
            var duration = stopwatchProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

            activity?.SetTag("http.status_code", (int?)response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                if (response.Content is not null)
                {
                    ApiMetrics.RecordSuccess(apiName, operationName, duration);

                    activity?.SetStatus(ActivityStatusCode.Ok);

                    return response.Content;
                }
                else
                {
                    // Handle null content on success status as a deserialization error
                    const string errorType = "DeserializationFailed";
                    ApiMetrics.RecordError(apiName, operationName, duration, errorType, (int?)response.StatusCode);

                    activity?.SetTag("error.type", "DeserializationFailed");
                    activity?.SetTag("error.message", "Response content was null despite success status.");
                    activity?.SetStatus(ActivityStatusCode.Error, "Deserialization failed: null content on success response.");

                    logger.LogDeserializationFailed(
                        apiName,
                        operationName,
                        (int)response.StatusCode.GetValueOrDefault(),
                        duration
                    );

                    return Error.Failure(
                        $"{apiName}.{operationName}.DeserializationFailed",
                        "API call succeeded but response content was null, indicating a deserialization failure."
                    );
                }
            }
            else
            {
                var statusCode = (int)response.StatusCode.GetValueOrDefault();
                var errorType = $"HttpError_{statusCode}";
                ApiMetrics.RecordError(apiName, operationName, duration, errorType, (int?)response.StatusCode);

                logger.LogApiError(
                    apiName,
                    operationName,
                    statusCode,
                    duration
                );

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return Error.NotFound(
                        $"{apiName}.{operationName}.NotFound",
                        "The requested resource was not found.");
                }

                // 4xx: the server has already produced a human-readable reason (validation failure,
                // conflict, etc.) - surface it instead of a bare status code. 5xx stays generic so we
                // never leak internal details to the user.
                var detail = statusCode is >= 400 and < 500 && response.HasResponseError(out var responseError)
                    ? TryExtractErrorDetail(responseError.Content)
                    : null;

                if (detail is not null)
                {
                    return statusCode == StatusCodes.Status409Conflict
                        ? Error.Conflict($"{apiName}.{operationName}.Conflict", detail)
                        : Error.Validation($"{apiName}.{operationName}.Validation", detail);
                }

                return Error.Failure(
                    $"{apiName}.{operationName}.HttpError",
                    "An unexpected error occurred. Please try again."
                );
            }
        }
        catch (ApiException ex)
        {
            return HandleException<TResponse>(
                apiName,
                operationName,
                startTimestamp,
                activity,
                ex,
                (int?)ex.StatusCode
            );
        }
        catch (HttpRequestException ex)
        {
            return HandleException<TResponse>(
                apiName,
                operationName,
                startTimestamp,
                activity,
                ex,
                httpStatusCode: null
            );
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var duration = stopwatchProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;
            ApiMetrics.RecordError(apiName, operationName, duration, "OperationCanceled");
            activity?.SetStatus(ActivityStatusCode.Error, "Operation canceled by caller.");

            logger.LogApiCancelled(
                apiName,
                operationName,
                duration
            );

            return Error.Failure($"{apiName}.{operationName}.Cancelled", "Request was canceled.");
        }
        catch (TaskCanceledException ex)
        {
            return HandleException<TResponse>(
                apiName,
                operationName,
                startTimestamp,
                activity,
                ex,
                httpStatusCode: null
            );
        }
        catch (Exception ex)
        {
            return HandleException<TResponse>(
                apiName,
                operationName,
                startTimestamp,
                activity,
                ex,
                httpStatusCode: null
            );
        }
    }

    /// <summary>
    /// Executes an API call that returns no response body (e.g. a 204 delete), reusing the generic
    /// overload's activity/metrics/error-mapping logic via a thin <see cref="IApiResponse{T}"/> adapter.
    /// </summary>
    public Task<ErrorOr<Success>> ExecuteAsync(
        string apiName,
        string operationName,
        Func<CancellationToken, Task<IApiResponse>> apiCall,
        CancellationToken cancellationToken = default)
        => ExecuteAsync<Success>(
            apiName,
            operationName,
            async ct => new SuccessApiResponse(await apiCall(ct)),
            cancellationToken);

    private ErrorOr<TResponse> HandleException<TResponse>(
        string apiName,
        string operationName,
        long startTimestamp,
        Activity? activity,
        Exception ex,
        int? httpStatusCode)
    {
        var duration = stopwatchProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;
        var errorType = ex.GetType().Name;

        ApiMetrics.RecordError(apiName, operationName, duration, errorType, httpStatusCode);

        // Stryker disable once NullCoalescing : activity is null in tests (no ActivitySource); tag mutations are untestable
        activity?.SetTag("error.type", ex.GetType().FullName ?? ex.GetType().Name);
        activity?.SetTag("error.message", ex.Message);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

        if (httpStatusCode.HasValue)
        {
            activity?.SetTag(ApiMetricTagNames.HttpStatusCode, httpStatusCode.Value);
        }

        logger.LogApiException(
            apiName,
            operationName,
            duration,
            ex
        );

        var detail = ex is ApiException apiException && httpStatusCode is >= 400 and < 500
            ? TryExtractErrorDetail(apiException.Content)
            : null;

        return Error.Failure($"{apiName}.{operationName}.Exception", detail ?? ex.Message);
    }

    /// <summary>
    /// Pulls a human-readable message out of the API's RFC 9457 <c>ProblemDetails</c> body
    /// (<c>{ "detail": ..., "errors": [{ "name": ..., "reason": "..." }] }</c> - FastEndpoints'
    /// <c>ErrOpts.UseProblemDetails()</c>), preferring the flattened validation/conflict reasons over the
    /// generic "detail" text. Returns null on anything that doesn't parse, so callers can fall back to a
    /// generic message instead of showing raw JSON.
    /// </summary>
    private static string? TryExtractErrorDetail(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ProblemDetailsPayload>(content, JsonSerializerOptions.Web);

            var reasons = payload?.Errors
                .Select(error => error.Reason)
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .ToList();

            if (reasons is { Count: > 0 })
            {
                return string.Join(" ", reasons);
            }

            return string.IsNullOrWhiteSpace(payload?.Detail) ? null : payload.Detail;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class ProblemDetailsPayload
    {
        public string Detail { get; init; } = string.Empty;

        public List<ProblemDetailsError> Errors { get; init; } = [];
    }

    private sealed class ProblemDetailsError
    {
        public string Reason { get; init; } = string.Empty;
    }

    /// <summary>
    /// Adapts a bodyless <see cref="IApiResponse"/> to <see cref="IApiResponse{Success}"/> so the
    /// non-generic <see cref="ExecuteAsync(string, string, Func{CancellationToken, Task{IApiResponse}}, CancellationToken)"/>
    /// overload can reuse the generic overload's activity/metrics/error-mapping logic verbatim.
    /// </summary>
    internal sealed class SuccessApiResponse(IApiResponse inner) : IApiResponse<Success>
    {
        public Success Content => Result.Success;
        public bool HasContent => inner.IsSuccessStatusCode;
        public bool IsSuccessfulWithContent => inner.IsSuccessStatusCode;
        public HttpResponseHeaders? Headers => inner.Headers;
        public HttpContentHeaders? ContentHeaders => inner.ContentHeaders;
        public bool IsSuccessStatusCode => inner.IsSuccessStatusCode;
        public bool IsSuccessful => inner.IsSuccessful;
        public bool IsReceived => inner.IsReceived;
        public HttpStatusCode? StatusCode => inner.StatusCode;
        public string? ReasonPhrase => inner.ReasonPhrase;
        public HttpRequestMessage? RequestMessage => inner.RequestMessage;
        public Version? Version => inner.Version;
        public ApiExceptionBase? Error => inner.Error;

        public bool HasRequestError([NotNullWhen(true)] out ApiRequestException? error)
            => inner.HasRequestError(out error);

        public bool HasResponseError([NotNullWhen(true)] out ApiException? error)
            => inner.HasResponseError(out error);

        public void Dispose() => inner.Dispose();
    }
}

internal static partial class ApiExecutorLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "API call failed: {ApiName}.{OperationName} returned status {StatusCode} (Duration: {DurationMs}ms)")]
    public static partial void LogApiError(this ILogger<ApiExecutor> logger, string apiName, string operationName, int statusCode, double durationMs);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "API deserialization failed: {ApiName}.{OperationName} returned status {StatusCode} with null content (Duration: {DurationMs}ms)")]
    public static partial void LogDeserializationFailed(this ILogger<ApiExecutor> logger, string apiName, string operationName, int statusCode, double durationMs);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "API call cancelled: {ApiName}.{OperationName} (Duration: {DurationMs}ms)")]
    public static partial void LogApiCancelled(this ILogger<ApiExecutor> logger, string apiName, string operationName, double durationMs);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "API call exception: {ApiName}.{OperationName} (Duration: {DurationMs}ms)")]
    public static partial void LogApiException(this ILogger<ApiExecutor> logger, string apiName, string operationName, double durationMs, Exception exception);
}