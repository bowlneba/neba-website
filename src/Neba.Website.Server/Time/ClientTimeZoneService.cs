using Microsoft.JSInterop;

namespace Neba.Website.Server.Time;

internal sealed class ClientTimeZoneService(IJSRuntime jsRuntime, ILogger<ClientTimeZoneService> logger)
    : IClientTimeZoneService, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private TimeZoneInfo? _timeZone;

    public async Task<DateTimeOffset> ToLocalAsync(DateTimeOffset utc)
    {
        var timeZone = await GetTimeZoneAsync();
        return TimeZoneInfo.ConvertTime(utc, timeZone);
    }

    public async Task<DateTimeOffset> ToUtcAsync(DateTime local)
    {
        var timeZone = await GetTimeZoneAsync();
        var utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);
        return new DateTimeOffset(utc);
    }

    private async Task<TimeZoneInfo> GetTimeZoneAsync()
    {
        if (_timeZone is not null)
        {
            return _timeZone;
        }

        try
        {
            _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/browser-time.js");
            var timeZoneId = await _module.InvokeAsync<string>("getTimeZoneId");
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogClientTimeZoneResolutionFailed(ex);
            _timeZone = TimeZoneInfo.Utc;
        }

        return _timeZone;
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already gone — nothing to clean up.
            }
        }
    }
}

internal static partial class ClientTimeZoneServiceLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to resolve the client's browser time zone; falling back to UTC.")]
    public static partial void LogClientTimeZoneResolutionFailed(this ILogger<ClientTimeZoneService> logger, Exception exception);
}