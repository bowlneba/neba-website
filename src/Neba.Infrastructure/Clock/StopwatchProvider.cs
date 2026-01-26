using System.Diagnostics;

using Neba.Application.Clock;

namespace Neba.Infrastructure.Clock;

internal sealed class StopwatchProvider
    : IStopwatchProvider
{
    public long GetTimestamp()
        => Stopwatch.GetTimestamp();

    public TimeSpan GetElapsedTime(long startTimestamp)
        => Stopwatch.GetElapsedTime(startTimestamp);
}