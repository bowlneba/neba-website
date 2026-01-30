using System.Diagnostics;

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

            activity?.SetTag("http.status_code", (int)response.StatusCode);

            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                ApiMetrics.RecordSuccess(apiName, operationName, duration);

                activity?.SetStatus(ActivityStatusCode.Ok);

                return response.Content;
            }

            var errorType = $"HttpError_{(int)response.StatusCode}";
            ApiMetrics.RecordError(apiName, operationName, duration, errorType, (int)response.StatusCode);

            logger.LogApiError(
                apiName,
                operationName,
                (int)response.StatusCode,
                duration
            );

            return Error.Failure(
                $"{apiName}.{operationName}.HttpError",
                $"API call failed with status code {(int)response.StatusCode}."
            );
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

        return Error.Failure($"{apiName}.{operationName}.Exception", ex.Message);
    }
}

internal static partial class ApiExecutorLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "API call failed: {ApiName}.{OperationName} returned status {StatusCode} (Duration: {DurationMs}ms)")]
    public static partial void LogApiError(this ILogger<ApiExecutor> logger, string apiName, string operationName, int statusCode, double durationMs);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "API call cancelled: {ApiName}.{OperationName} (Duration: {DurationMs}ms)")]
    public static partial void LogApiCancelled(this ILogger<ApiExecutor> logger, string apiName, string operationName, double durationMs);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "API call exception: {ApiName}.{OperationName} (Duration: {DurationMs}ms)")]
    public static partial void LogApiException(this ILogger<ApiExecutor> logger, string apiName, string operationName, double durationMs, Exception exception);
}