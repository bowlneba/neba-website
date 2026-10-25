using System.Diagnostics.Metrics;

using Neba.Api.Security;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security;

[UnitTest]
[Component("Security")]
public sealed class SecurityMetricsTests
{
    // ShouldContain (not ShouldHaveSingleItem): other tests call the same static methods while this listener is active.

    [Fact(DisplayName = "RecordResetRequest should add one count tagged with the result")]
    public void RecordResetRequest_ShouldAddCount_WithResultTag()
    {
        // Arrange
        var measurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();
        using var listener = BuildListener(measurements);

        // Act
        SecurityMetrics.RecordResetRequest("cooldown");

        // Assert
        measurements.ShouldContain(m =>
            m.Instrument == SecurityMetrics.ResetRequestCountName &&
            m.Value == 1 &&
            m.Tags.Any(t => t.Key == "result" && (string?)t.Value == "cooldown"));
    }

    [Fact(DisplayName = "RecordPasswordChange should add one count tagged with the result")]
    public void RecordPasswordChange_ShouldAddCount_WithResultTag()
    {
        // Arrange
        var measurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();
        using var listener = BuildListener(measurements);

        // Act
        SecurityMetrics.RecordPasswordChange("locked_out");

        // Assert
        measurements.ShouldContain(m =>
            m.Instrument == SecurityMetrics.PasswordChangeCountName &&
            m.Value == 1 &&
            m.Tags.Any(t => t.Key == "result" && (string?)t.Value == "locked_out"));
    }

    private static MeterListener BuildListener(List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)> measurements)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == SecurityMetrics.MeterName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            lock (measurements)
            {
                measurements.Add((instrument.Name, value, tags.ToArray()));
            }
        });

        listener.Start();
        return listener;
    }
}