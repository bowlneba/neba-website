using System.Diagnostics;

namespace Neba.Website.Server.Clock;

internal sealed class StopwatchProvider
    : IStopwatchProvider
{
    public long GetTimestamp()
        => Stopwatch.GetTimestamp();

    public TimeSpan GetElapsedTime(long startTimestamp)
        => Stopwatch.GetElapsedTime(startTimestamp);
}

internal interface IStopwatchProvider
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