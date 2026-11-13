using System.Diagnostics.Metrics;

namespace Neba.Api.Tests.Features.HallOfFame;

/// <summary>
/// Records the Hall of Fame points cache-miss counter. The counter is process-wide, so tests assert the
/// recording contains a miss for their query, not an exact total.
/// </summary>
internal sealed class CacheMissRecorder : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly List<string> _queries = [];
    private readonly Lock _lock = new();

    public CacheMissRecorder()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Neba.HallOfFame" && instrument.Name == "neba.hall_of_fame.points.cache_miss.count")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "query" && tag.Value is string query)
                {
                    lock (_lock)
                    {
                        _queries.Add(query);
                    }
                }
            }
        });

        _listener.Start();
    }

    public IReadOnlyList<string> Queries
    {
        get
        {
            lock (_lock)
            {
                return [.. _queries];
            }
        }
    }

    public void Dispose() => _listener.Dispose();
}
