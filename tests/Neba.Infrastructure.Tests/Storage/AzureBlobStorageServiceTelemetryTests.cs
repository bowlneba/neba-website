using System.Diagnostics;
using System.Diagnostics.Metrics;

using Microsoft.Extensions.Logging.Abstractions;

using Neba.Infrastructure.Clock;
using Neba.Infrastructure.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

namespace Neba.Infrastructure.Tests.Storage;

[IntegrationTest]
[Component("Storage")]
[Collection<AzuriteFixture>]
public sealed class AzureBlobStorageServiceTelemetryTests : IClassFixture<AzuriteFixture>, IDisposable
{
    private readonly AzureBlobStorageService _sut;
    private readonly List<Activity> _recordedActivities;
    private readonly ActivityListener _activityListener;
    private readonly List<MetricMeasurement> _recordedMetrics;
    private readonly MeterListener _meterListener;

    public AzureBlobStorageServiceTelemetryTests(AzuriteFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _sut = new AzureBlobStorageService(
            fixture.BlobServiceClient,
            new StopwatchProvider(),
            NullLogger<AzureBlobStorageService>.Instance);

        _recordedActivities = [];
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Neba.Storage",
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _recordedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);

        _recordedMetrics = [];
        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Neba.Storage")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _meterListener.Start();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    private void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? _)
            where T : struct
    {
        _recordedMetrics.Add(new MetricMeasurement(
            instrument.Name,
            Convert.ToDouble(measurement, System.Globalization.CultureInfo.InvariantCulture),
            tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)));
    }

    private sealed record MetricMeasurement(
        string InstrumentName,
        double Value,
        Dictionary<string, string> Tags);

    private static string UniqueContainer() => $"test-{Guid.NewGuid():N}";

    [Fact(DisplayName = "UploadFileAsync should create activity with correct name and tags")]
    public async Task UploadFileAsync_ShouldCreateActivityWithCorrectNameAndTags()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();
        const string path = "telemetry-test.txt";

        // Act
        await _sut.UploadFileAsync(
            container,
            path,
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None);

        // Assert
        _recordedActivities.ShouldHaveSingleItem();
        Activity activity = _recordedActivities[0];

        activity.DisplayName.ShouldBe("storage.upload");
        activity.GetTagItem("code.function").ShouldBe("UploadFileAsync");
        activity.GetTagItem("code.namespace").ShouldBe("Neba.Storage");
        activity.GetTagItem("storage.container").ShouldBe(container);
        activity.GetTagItem("storage.path").ShouldBe(path);
        activity.GetTagItem("storage.size_bytes").ShouldBe(StoredFileFactory.ValidContent.Length);
        activity.GetTagItem("storage.content_type").ShouldBe(StoredFileFactory.ValidContentType);
        activity.GetTagItem("storage.duration_ms").ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact(DisplayName = "UploadFileAsync should record success metrics")]
    public async Task UploadFileAsync_ShouldRecordSuccessMetrics()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();
        const string path = "metrics-test.txt";

        // Act
        await _sut.UploadFileAsync(
            container,
            path,
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None);

        // Assert
        List<MetricMeasurement> countMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.storage.operation.count")];
        countMetrics.ShouldHaveSingleItem();
        countMetrics[0].Value.ShouldBe(1);
        countMetrics[0].Tags["storage.container"].ShouldBe(container);
        countMetrics[0].Tags["storage.operation"].ShouldBe("upload");
        countMetrics[0].Tags["result"].ShouldBe("success");

        List<MetricMeasurement> durationMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.storage.operation.duration")];
        durationMetrics.ShouldHaveSingleItem();
        durationMetrics[0].Value.ShouldBeGreaterThanOrEqualTo(0);
        durationMetrics[0].Tags["storage.container"].ShouldBe(container);
        durationMetrics[0].Tags["storage.operation"].ShouldBe("upload");
        durationMetrics[0].Tags["result"].ShouldBe("success");

        List<MetricMeasurement> sizeMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.storage.file.size")];
        sizeMetrics.ShouldHaveSingleItem();
        sizeMetrics[0].Value.ShouldBe(StoredFileFactory.ValidContent.Length);
        sizeMetrics[0].Tags["storage.container"].ShouldBe(container);
        sizeMetrics[0].Tags["storage.operation"].ShouldBe("upload");
    }

    [Fact(DisplayName = "GetFileAsync should create activity with correct tags when file exists")]
    public async Task GetFileAsync_ShouldCreateActivityWithCorrectTags_WhenFileExists()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();
        const string path = "download-test.txt";

        await _sut.UploadFileAsync(
            container,
            path,
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None);

        _recordedActivities.Clear();
        _recordedMetrics.Clear();

        // Act
        await _sut.GetFileAsync(container, path, CancellationToken.None);

        // Assert
        _recordedActivities.ShouldHaveSingleItem();
        Activity activity = _recordedActivities[0];

        activity.DisplayName.ShouldBe("storage.download");
        activity.GetTagItem("code.function").ShouldBe("GetFileAsync");
        activity.GetTagItem("code.namespace").ShouldBe("Neba.Storage");
        activity.GetTagItem("storage.container").ShouldBe(container);
        activity.GetTagItem("storage.path").ShouldBe(path);
        activity.GetTagItem("storage.found").ShouldBe(true);
        activity.GetTagItem("storage.size_bytes").ShouldBe(StoredFileFactory.ValidContent.Length);
        activity.GetTagItem("storage.content_type").ShouldBe(StoredFileFactory.ValidContentType);
        activity.GetTagItem("storage.duration_ms").ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact(DisplayName = "GetFileAsync should create activity with found=false when file does not exist")]
    public async Task GetFileAsync_ShouldCreateActivityWithFoundFalse_WhenFileDoesNotExist()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();

        // Act
        await _sut.GetFileAsync(container, "nonexistent.txt", CancellationToken.None);

        // Assert
        _recordedActivities.ShouldHaveSingleItem();
        Activity activity = _recordedActivities[0];

        activity.DisplayName.ShouldBe("storage.download");
        activity.GetTagItem("storage.found").ShouldBe(false);
        activity.GetTagItem("storage.duration_ms").ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact(DisplayName = "GetFileAsync should record success metrics when file exists")]
    public async Task GetFileAsync_ShouldRecordSuccessMetrics_WhenFileExists()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();
        const string path = "download-metrics-test.txt";

        await _sut.UploadFileAsync(
            container,
            path,
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None);

        _recordedActivities.Clear();
        _recordedMetrics.Clear();

        // Act
        await _sut.GetFileAsync(container, path, CancellationToken.None);

        // Assert
        List<MetricMeasurement> countMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.storage.operation.count")];
        countMetrics.ShouldHaveSingleItem();
        countMetrics[0].Value.ShouldBe(1);
        countMetrics[0].Tags["storage.container"].ShouldBe(container);
        countMetrics[0].Tags["storage.operation"].ShouldBe("download");
        countMetrics[0].Tags["result"].ShouldBe("success");

        List<MetricMeasurement> sizeMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.storage.file.size")];
        sizeMetrics.ShouldHaveSingleItem();
        sizeMetrics[0].Value.ShouldBe(StoredFileFactory.ValidContent.Length);
    }

    [Fact(DisplayName = "ExistsAsync should create activity with correct tags")]
    public async Task ExistsAsync_ShouldCreateActivityWithCorrectTags()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();
        const string path = "exists-test.txt";

        await _sut.UploadFileAsync(
            container,
            path,
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None);

        _recordedActivities.Clear();
        _recordedMetrics.Clear();

        // Act
        await _sut.ExistsAsync(container, path, CancellationToken.None);

        // Assert
        _recordedActivities.ShouldHaveSingleItem();
        Activity activity = _recordedActivities[0];

        activity.DisplayName.ShouldBe("storage.exists");
        activity.GetTagItem("code.function").ShouldBe("ExistsAsync");
        activity.GetTagItem("code.namespace").ShouldBe("Neba.Storage");
        activity.GetTagItem("storage.container").ShouldBe(container);
        activity.GetTagItem("storage.path").ShouldBe(path);
        activity.GetTagItem("storage.exists").ShouldBe(true);
        activity.GetTagItem("storage.duration_ms").ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact(DisplayName = "ExistsAsync should record success metrics")]
    public async Task ExistsAsync_ShouldRecordSuccessMetrics()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();

        // Act
        await _sut.ExistsAsync(container, "nonexistent.txt", CancellationToken.None);

        // Assert
        List<MetricMeasurement> countMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.storage.operation.count")];
        countMetrics.ShouldHaveSingleItem();
        countMetrics[0].Value.ShouldBe(1);
        countMetrics[0].Tags["storage.container"].ShouldBe(container);
        countMetrics[0].Tags["storage.operation"].ShouldBe("exists");
        countMetrics[0].Tags["result"].ShouldBe("success");

        List<MetricMeasurement> durationMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.storage.operation.duration")];
        durationMetrics.ShouldHaveSingleItem();
        durationMetrics[0].Value.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact(DisplayName = "UploadFileAsync should record all expected metrics")]
    public async Task UploadFileAsync_ShouldRecordAllExpectedMetrics()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();

        // Act
        await _sut.UploadFileAsync(
            container,
            "all-metrics-test.txt",
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None);

        // Assert
        _recordedMetrics.Select(m => m.InstrumentName).Distinct().ShouldBe(
            [
                "neba.storage.operation.count",
                "neba.storage.operation.duration",
                "neba.storage.file.size"
            ],
            ignoreOrder: true);
    }

    [Fact(DisplayName = "Activity duration should match metric duration approximately")]
    public async Task UploadFileAsync_ShouldHaveMatchingDuration_WhenActivityAndMetricRecorded()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        var container = UniqueContainer();

        // Act
        await _sut.UploadFileAsync(
            container,
            "duration-test.txt",
            StoredFileFactory.ValidContent,
            StoredFileFactory.ValidContentType,
            new Dictionary<string, string>(StoredFileFactory.ValidMetadata),
            CancellationToken.None);

        // Assert
        Activity activity = _recordedActivities[0];

        double activityDuration = Convert.ToDouble(activity.GetTagItem("storage.duration_ms"), System.Globalization.CultureInfo.InvariantCulture);

        MetricMeasurement durationMetric = _recordedMetrics
            .Single(m => m.InstrumentName == "neba.storage.operation.duration");

        // Allow for small timing differences (within 5ms)
        Math.Abs(activityDuration - durationMetric.Value).ShouldBeLessThan(5.0);
    }
}
