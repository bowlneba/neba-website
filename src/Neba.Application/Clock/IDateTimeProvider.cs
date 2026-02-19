namespace Neba.Application.Clock;

/// <summary>
/// Provides the current date.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current date.
    /// </summary>
    DateOnly Today { get; }

    /// <summary>
    /// Gets the current date and time in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}