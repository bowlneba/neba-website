using System.Diagnostics.Metrics;

using Neba.Api.Features.Documents.SyncDocument;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Documents.SyncDocument;

[UnitTest]
[Component("Documents")]
public sealed class SyncDocumentToStorageMetricsTests
{
    private const string MeterName = "Neba.BackgroundJobs";

    // Use ShouldContain (not ShouldHaveSingleItem) so tests are parallel-safe:
    // other test classes may call the same static methods while these listeners are active.

    [Fact(DisplayName = "RecordJobStarting should add one execution count with document name and triggered-by tags")]
    public void RecordJobStarting_ShouldAddExecutionCount_WithCorrectTags()
    {
        var longMeasurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();

        using var listener = BuildListener(longMeasurements: longMeasurements);

        SyncDocumentToStorageMetrics.RecordJobStarting("bylaws", "scheduled");

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.background_job.sync_document.executions" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == "bylaws") &&
            m.Tags.Any(t => t.Key == "triggered.by" && (string?)t.Value == "scheduled"));
    }

    [Fact(DisplayName = "RecordJobSuccess should add success count and duration with result=success tags")]
    public void RecordJobSuccess_ShouldAddSuccessCountAndDuration_WithCorrectTags()
    {
        var longMeasurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();
        var doubleMeasurements = new List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)>();

        using var listener = BuildListener(longMeasurements, doubleMeasurements);

        SyncDocumentToStorageMetrics.RecordJobSuccess("bylaws", 42.5);

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.successes" &&
            m.Value == 1L &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == "bylaws"));

        doubleMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.duration" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == "bylaws") &&
            m.Tags.Any(t => t.Key == "result" && (string?)t.Value == "success"));
    }

    [Fact(DisplayName = "RecordJobFailure should add failure count and duration with error type and result=failure tags")]
    public void RecordJobFailure_ShouldAddFailureCountAndDuration_WithCorrectTags()
    {
        var longMeasurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();
        var doubleMeasurements = new List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)>();

        using var listener = BuildListener(longMeasurements, doubleMeasurements);

        SyncDocumentToStorageMetrics.RecordJobFailure("bylaws", 33.0, "InvalidOperationException");

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.failures" &&
            m.Value == 1L &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == "bylaws") &&
            m.Tags.Any(t => t.Key == "error.type" && (string?)t.Value == "InvalidOperationException"));

        doubleMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.duration" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == "bylaws") &&
            m.Tags.Any(t => t.Key == "result" && (string?)t.Value == "failure") &&
            m.Tags.Any(t => t.Key == "error.type" && (string?)t.Value == "InvalidOperationException"));
    }

    [Fact(DisplayName = "RecordRetrieveDuration should record retrieve histogram with document name tag")]
    public void RecordRetrieveDuration_ShouldRecordHistogram_WithCorrectTags()
    {
        var doubleMeasurements = new List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)>();

        using var listener = BuildListener(doubleMeasurements: doubleMeasurements);

        SyncDocumentToStorageMetrics.RecordRetrieveDuration("bylaws", 15.0);

        doubleMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.retrieve.duration" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == "bylaws"));
    }

    [Fact(DisplayName = "RecordUploadDuration should record upload histogram with document name tag")]
    public void RecordUploadDuration_ShouldRecordHistogram_WithCorrectTags()
    {
        var doubleMeasurements = new List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)>();

        using var listener = BuildListener(doubleMeasurements: doubleMeasurements);

        SyncDocumentToStorageMetrics.RecordUploadDuration("bylaws", 22.0);

        doubleMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.upload.duration" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == "bylaws"));
    }

    internal static MeterListener BuildListener(
        List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>? longMeasurements = null,
        List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)>? doubleMeasurements = null)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == MeterName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };

        if (longMeasurements is not null)
        {
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
                longMeasurements.Add((instrument.Name, measurement, tags.ToArray())));
        }

        if (doubleMeasurements is not null)
        {
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
                doubleMeasurements.Add((instrument.Name, measurement, tags.ToArray())));
        }

        listener.Start();
        return listener;
    }
}