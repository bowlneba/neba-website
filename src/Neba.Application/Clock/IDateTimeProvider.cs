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
}