using System.Diagnostics;

using Microsoft.Extensions.Logging.Abstractions;

using Neba.Application.Clock;
using Neba.Application.Documents;
using Neba.Application.Documents.SyncDocument;
using Neba.Application.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Documents;

namespace Neba.Application.Tests.Documents.SyncDocument;

[UnitTest]
[Component("Documents")]
public sealed class SyncDocumentToStorageJobHandlerTests
{
    private readonly Mock<IDocumentsService> _documentsServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IStopwatchProvider> _stopwatchProviderMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    private readonly SyncDocumentToStorageJobHandler _handler;

    public SyncDocumentToStorageJobHandlerTests()
    {
        _documentsServiceMock = new Mock<IDocumentsService>(MockBehavior.Strict);
        _fileStorageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        _stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        _dateTimeProviderMock = new Mock<IDateTimeProvider>(MockBehavior.Strict);

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(1000L);

        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(It.IsAny<long>()))
            .Returns(TimeSpan.FromMilliseconds(50));

        _handler = new SyncDocumentToStorageJobHandler(
            _documentsServiceMock.Object,
            _fileStorageServiceMock.Object,
            _stopwatchProviderMock.Object,
            _dateTimeProviderMock.Object,
            NullLogger<SyncDocumentToStorageJobHandler>.Instance);
    }

    [Fact(DisplayName = "Should retrieve document and upload to storage on success")]
    public async Task ExecuteAsync_ShouldRetrieveAndUpload_WhenDocumentExists()
    {
        // Arrange
        var modifiedAt = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var document = DocumentDtoFactory.Create(modifiedAt: modifiedAt);
        var cachedAt = new DateTimeOffset(2026, 2, 17, 7, 0, 0, TimeSpan.Zero);
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = document.Name,
            TriggeredBy = "scheduled"
        };

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(job.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(document);

        _dateTimeProviderMock
            .Setup(d => d.UtcNow)
            .Returns(cachedAt);

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                "documents",
                job.DocumentName,
                document.Content,
                document.ContentType,
                It.Is<IDictionary<string, string>>(m =>
                    m["source_document_id"] == document.Id &&
                    m["cached_at"] == cachedAt.ToString("o") &&
                    m["source_last_modified"] == modifiedAt.ToString("o")),
                TestContext.Current.CancellationToken))
            .Returns(Task.CompletedTask);

        var longMeasurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();
        var doubleMeasurements = new List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = SyncDocumentToStorageMetricsTests.BuildListener(longMeasurements, doubleMeasurements);

        var (activityListener, activities) = BuildActivityListener();
        using var __ = activityListener;

        // Act
        await Should.NotThrowAsync(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        // Assert metrics — use ShouldContain (not ShouldHaveSingleItem) for parallel-test safety.
        // Filter by document name to avoid capturing measurements from other parallel test classes.
        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.background_job.sync_document.executions" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName));

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.successes" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName));

        doubleMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.duration" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName) &&
            m.Tags.Any(t => t.Key == "result" && (string?)t.Value == "success"));

        doubleMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.retrieve.duration" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName));

        // Assert activity tags and status
        var activity = activities.First(a => a.Tags.Any(t => t.Key == "document.name" && t.Value == job.DocumentName));
        activity.Tags.ShouldContain(kvp => kvp.Key == "document.name" && kvp.Value == job.DocumentName);
        activity.Tags.ShouldContain(kvp => kvp.Key == "triggered.by" && kvp.Value == job.TriggeredBy);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact(DisplayName = "Should return without throwing when document is not found")]
    public async Task ExecuteAsync_ShouldReturnGracefully_WhenDocumentNotFound()
    {
        // Arrange
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = "non-existent",
            TriggeredBy = "scheduled"
        };

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(job.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync((DocumentDto?)null);

        var longMeasurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();
        var doubleMeasurements = new List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = SyncDocumentToStorageMetricsTests.BuildListener(longMeasurements, doubleMeasurements);

        var (activityListener, activities) = BuildActivityListener();
        using var __ = activityListener;

        // Act
        await Should.NotThrowAsync(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        // Assert metrics
        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.background_job.sync_document.executions" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName));

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.failures" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName) &&
            m.Tags.Any(t => t.Key == "error.type" && (string?)t.Value == "DocumentNotFound"));

        doubleMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.duration" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName) &&
            m.Tags.Any(t => t.Key == "result" && (string?)t.Value == "failure"));

        // Assert activity status
        var activity = activities.First(a => a.Tags.Any(t => t.Key == "document.name" && t.Value == job.DocumentName));
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Document not found");
    }

    [Fact(DisplayName = "Should propagate exception when upload fails")]
    public async Task ExecuteAsync_ShouldPropagateException_WhenUploadFails()
    {
        // Arrange
        var document = DocumentDtoFactory.Create();
        var cachedAt = new DateTimeOffset(2026, 2, 17, 7, 0, 0, TimeSpan.Zero);
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = document.Name,
            TriggeredBy = "scheduled"
        };

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(job.DocumentName, TestContext.Current.CancellationToken))
            .ReturnsAsync(document);

        _dateTimeProviderMock
            .Setup(d => d.UtcNow)
            .Returns(cachedAt);

        _fileStorageServiceMock
            .Setup(s => s.UploadFileAsync(
                "documents",
                job.DocumentName,
                document.Content,
                document.ContentType,
                It.IsAny<IDictionary<string, string>>(),
                TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("Storage unavailable"));

        var longMeasurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();
        var doubleMeasurements = new List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = SyncDocumentToStorageMetricsTests.BuildListener(longMeasurements, doubleMeasurements);

        var (activityListener, activities) = BuildActivityListener();
        using var __ = activityListener;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        exception.Message.ShouldBe("Storage unavailable");

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.background_job.sync_document.executions" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName));

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.failures" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName) &&
            m.Tags.Any(t => t.Key == "error.type" && (string?)t.Value == "InvalidOperationException"));

        // Assert activity status
        var activity = activities.First(a => a.Tags.Any(t => t.Key == "document.name" && t.Value == job.DocumentName));
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Storage unavailable");
    }

    private static (ActivityListener Listener, List<Activity> Activities) BuildActivityListener()
    {
        var activities = new List<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Neba.BackgroundJobs",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(listener);
        return (listener, activities);
    }

    [Fact(DisplayName = "Should propagate exception when document retrieval fails")]
    public async Task ExecuteAsync_ShouldPropagateException_WhenRetrievalFails()
    {
        // Arrange
        var job = new SyncDocumentToStorageJob
        {
            DocumentName = "bylaws",
            TriggeredBy = "scheduled"
        };

        _documentsServiceMock
            .Setup(s => s.GetDocumentAsHtmlAsync(job.DocumentName, TestContext.Current.CancellationToken))
            .ThrowsAsync(new HttpRequestException("Google Drive API error"));

        var longMeasurements = new List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = SyncDocumentToStorageMetricsTests.BuildListener(longMeasurements: longMeasurements);

        // Act & Assert
        var exception = await Should.ThrowAsync<HttpRequestException>(
            () => _handler.ExecuteAsync(job, TestContext.Current.CancellationToken));

        exception.Message.ShouldBe("Google Drive API error");

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.background_job.sync_document.executions" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName));

        longMeasurements.ShouldContain(m =>
            m.Instrument == "neba.backgroundjob.sync_document.failures" &&
            m.Tags.Any(t => t.Key == "document.name" && (string?)t.Value == job.DocumentName) &&
            m.Tags.Any(t => t.Key == "error.type" && (string?)t.Value == "HttpRequestException"));
    }
}