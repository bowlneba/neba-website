namespace Neba.Website.Server.Time;

/// <summary>
/// Resolves the current viewer's browser-local time zone and converts between it and UTC.
/// Scoped per circuit — the underlying JS lookup happens at most once per session.
/// </summary>
public interface IClientTimeZoneService
{
    /// <summary>
    /// Converts a UTC instant to the viewer's local time zone.
    /// </summary>
    Task<DateTimeOffset> ToLocalAsync(DateTimeOffset utc);

    /// <summary>
    /// Converts a local wall-clock value (as captured from a plain <c>datetime-local</c> input,
    /// with no time zone of its own) to UTC, using the viewer's browser-local time zone.
    /// </summary>
    Task<DateTimeOffset> ToUtcAsync(DateTime local);
}