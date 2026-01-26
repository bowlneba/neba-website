namespace Neba.Application.Clock;

/// <summary>
/// Provides stopwatch functionality for measuring elapsed time.
/// </summary>
public interface IStopwatchProvider
{
    /// <summary>
    /// Gets the current timestamp.
    /// </summary>
    long GetTimestamp();

    /// <summary>
    /// Gets the elapsed time since the <paramref name="startTimestamp"/> value retrieved using <see cref="GetTimestamp"/>.
    /// </summary>
    /// <param name="startTimestamp">The starting timestamp.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the elapsed time.</returns>
    TimeSpan GetElapsedTime(long startTimestamp);
}