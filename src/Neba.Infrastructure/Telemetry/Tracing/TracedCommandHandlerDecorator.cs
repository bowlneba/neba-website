using System.Diagnostics;

using ErrorOr;

using Microsoft.Extensions.Logging;

using Neba.Application.Clock;
using Neba.Application.Messaging;

namespace Neba.Infrastructure.Telemetry.Tracing;

internal sealed class TracedCommandHandlerDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IStopwatchProvider stopwatchProvider,
    ILogger<TracedCommandHandlerDecorator<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private static readonly ActivitySource ActivitySource 
        = new("Neba.Handlers");

    private readonly ICommandHandler<TCommand, TResponse> _innerHandler = innerHandler;
    private readonly IStopwatchProvider _stopwatchProvider = stopwatchProvider;
    private readonly ILogger<TracedCommandHandlerDecorator<TCommand, TResponse>> _logger = logger;
    private readonly string _commandType = typeof(TCommand).Name;
    private readonly string _responseType = typeof(TResponse).Name;

    public async Task<ErrorOr<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        using Activity? activity = ActivitySource.StartActivity($"command.{_commandType}", ActivityKind.Server);

        activity?.SetCodeAttributes(_commandType, "Neba.Handlers");
        activity?.SetTag("handler.type", "command");
        activity?.SetTag("response.type", _responseType);

        var startTimestamp = _stopwatchProvider.GetTimestamp();

        try
        {
            var result = await _innerHandler.HandleAsync(command, cancellationToken);

            double durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

            activity?.SetTag("command.duration_ms", durationMs);

            if (result.IsError)
            {
                activity?.SetTag("command.success", false);
                activity?.SetTag("error.count", result.Errors.Count);
                activity?.SetTag("error.codes", string.Join(",", result.Errors.Select(e => e.Code)));
                activity?.SetStatus(ActivityStatusCode.Error, result.FirstError.Description);

                _logger.LogCommandExecutionReturnedErrors(
                    _commandType,
                    durationMs,
                    result.Errors.Count);
            }
            else
            {
                activity?.SetTag("command.success", true);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            return result;
        }
        catch (Exception ex)
        {
            double durationMs = _stopwatchProvider.GetElapsedTime(startTimestamp).TotalMilliseconds;

            activity?.SetTag("command.duration_ms", durationMs);
            activity?.SetTag("command.success", false);
            activity?.SetExceptionTags(ex);
            
            _logger.LogCommandExecutionFailed(
                _commandType,
                durationMs,
                ex);

            throw;
        }
    }
}

internal static partial class TracedCommandHandlerDecoratorLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Command execution returned errors: {CommandType} (Duration: {DurationMs}ms, ErrorCount: {ErrorCount})")]
    public static partial void LogCommandExecutionReturnedErrors(
        this ILogger logger,
        string commandType,
        double durationMs,
        int errorCount);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Command execution failed: {CommandType} (Duration: {DurationMs}ms)")]
    public static partial void LogCommandExecutionFailed(
        this ILogger logger,
        string commandType,
        double durationMs,
        Exception exception);
}