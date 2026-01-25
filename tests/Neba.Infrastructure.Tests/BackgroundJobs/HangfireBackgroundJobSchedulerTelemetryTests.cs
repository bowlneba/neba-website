using System.Diagnostics;
using System.Diagnostics.Metrics;

using Hangfire;
using Hangfire.InMemory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Application.BackgroundJobs;
using Neba.Infrastructure.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.BackgroundJobs;

[IntegrationTest]
[Component("Infrastructure.BackgroundJobs")]
[Collection("HangfireSequential")]
public sealed class HangfireBackgroundJobSchedulerTelemetryTests : IDisposable
{
    private readonly InMemoryStorage _jobStorage;
    private readonly List<Activity> _recordedActivities;
    private readonly ActivityListener _activityListener;
    private readonly List<MetricMeasurement> _recordedMetrics;
    private readonly MeterListener _meterListener;

    public HangfireBackgroundJobSchedulerTelemetryTests()
    {
        _jobStorage = new InMemoryStorage();
        JobStorage.Current = _jobStorage;

        _recordedActivities = [];
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Neba.Hangfire",
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _recordedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);

        _recordedMetrics = [];
        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Neba.Hangfire")
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
        _jobStorage.Dispose();
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

    private sealed record TestJob(string Name) : IBackgroundJob
    {
        public string JobName => $"Test Job: {Name}";
    }

    private sealed record SuccessfulJob : IBackgroundJob
    {
        public string JobName => "SuccessfulJob";
    }

    private sealed record FailingJob : IBackgroundJob
    {
        public string JobName => "FailingJob";
    }

    private sealed class TestJobHandler : IBackgroundJobHandler<TestJob>
    {
        public Task ExecuteAsync(TestJob job, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessfulJobHandler : IBackgroundJobHandler<SuccessfulJob>
    {
        public async Task ExecuteAsync(SuccessfulJob job, CancellationToken cancellationToken)
        {
            // Simulate some work to ensure duration is measurable
            await Task.Delay(10, cancellationToken);
        }
    }

    private sealed class FailingJobHandler : IBackgroundJobHandler<FailingJob>
    {
        public Task ExecuteAsync(FailingJob job, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Job execution failed");
        }
    }

    private static HangfireBackgroundJobScheduler CreateScheduler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBackgroundJobHandler<TestJob>, TestJobHandler>();
        services.AddSingleton<IBackgroundJobHandler<SuccessfulJob>, SuccessfulJobHandler>();
        services.AddSingleton<IBackgroundJobHandler<FailingJob>, FailingJobHandler>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        return new HangfireBackgroundJobScheduler(
            scopeFactory,
            NullLogger<HangfireBackgroundJobScheduler>.Instance);
    }

    private Activity GetActivityForJob(string jobTypeName)
    {
        List<Activity> relevantActivities = [.. _recordedActivities.Where(a => a.DisplayName == $"hangfire.execute_job.{jobTypeName}")];

        relevantActivities.ShouldHaveSingleItem();
        return relevantActivities[0];
    }

    [Fact(DisplayName = "Should create activity with correct name and tags when job succeeds")]
    public async Task ExecuteJobAsync_ShouldCreateActivityWithCorrectNameAndTags_WhenJobSucceedsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new SuccessfulJob();

        // Act
        await scheduler.ExecuteJobAsync(job, "SuccessfulJob", CancellationToken.None);

        // Assert
        Activity activity = GetActivityForJob("SuccessfulJob");

        activity.DisplayName.ShouldBe("hangfire.execute_job.SuccessfulJob");
        activity.GetTagItem("code.function").ShouldBe("SuccessfulJob");
        activity.GetTagItem("code.namespace").ShouldBe("Neba.Hangfire");
        activity.GetTagItem("job.display_name").ShouldBe("SuccessfulJob");
        activity.GetTagItem("job.duration_ms").ShouldNotBeNull();

        double duration = Convert.ToDouble(activity.GetTagItem("job.duration_ms"), System.Globalization.CultureInfo.InvariantCulture);
        duration.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact(DisplayName = "Should set activity status to Ok when job succeeds")]
    public async Task ExecuteJobAsync_ShouldSetActivityStatusToOk_WhenJobSucceedsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new SuccessfulJob();

        // Act
        await scheduler.ExecuteJobAsync(job, "SuccessfulJob", CancellationToken.None);

        // Assert
        Activity activity = GetActivityForJob("SuccessfulJob");

        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact(DisplayName = "Should set activity status to Error with exception tags when job fails")]
    public async Task ExecuteJobAsync_ShouldSetActivityStatusToError_WhenJobFailsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new FailingJob();

        // Act
        Func<Task> act = async () => await scheduler.ExecuteJobAsync(job, "FailingJob", CancellationToken.None);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();

        Activity activity = GetActivityForJob("FailingJob");

        activity.DisplayName.ShouldBe("hangfire.execute_job.FailingJob");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Job execution failed");
        activity.GetTagItem("error.type").ShouldBe("System.InvalidOperationException");
        activity.GetTagItem("error.message").ShouldBe("Job execution failed");
        activity.GetTagItem("error.stack_trace").ShouldNotBeNull();
        activity.GetTagItem("job.duration_ms").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should include display name in activity tags")]
    public async Task ExecuteJobAsync_ShouldIncludeDisplayNameInTags_WhenCustomDisplayNameProvidedAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new TestJob("CustomName");

        // Act
        await scheduler.ExecuteJobAsync(job, "Test Job: CustomName", CancellationToken.None);

        // Assert
        Activity activity = GetActivityForJob("TestJob");

        activity.GetTagItem("job.display_name").ShouldBe("Test Job: CustomName");
    }

    [Fact(DisplayName = "Should increment executions counter on job start")]
    public async Task ExecuteJobAsync_ShouldIncrementExecutionsCounter_OnJobStartAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new SuccessfulJob();

        // Act
        await scheduler.ExecuteJobAsync(job, "SuccessfulJob", CancellationToken.None);

        // Assert
        List<MetricMeasurement> executionMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.hangfire.job.executions")];

        executionMetrics.ShouldHaveSingleItem();
        executionMetrics[0].Value.ShouldBe(1);
        executionMetrics[0].Tags["job.type"].ShouldBe("SuccessfulJob");
    }

    [Fact(DisplayName = "Should increment successes counter when job succeeds")]
    public async Task ExecuteJobAsync_ShouldIncrementSuccessesCounter_WhenJobSucceedsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new SuccessfulJob();

        // Act
        await scheduler.ExecuteJobAsync(job, "SuccessfulJob", CancellationToken.None);

        // Assert
        List<MetricMeasurement> successMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.hangfire.job.successes")];

        successMetrics.ShouldHaveSingleItem();
        successMetrics[0].Value.ShouldBe(1);
        successMetrics[0].Tags["job.type"].ShouldBe("SuccessfulJob");
    }

    [Fact(DisplayName = "Should increment failures counter with error type when job fails")]
    public async Task ExecuteJobAsync_ShouldIncrementFailuresCounter_WhenJobFailsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new FailingJob();

        // Act
        Func<Task> act = async () => await scheduler.ExecuteJobAsync(job, "FailingJob", CancellationToken.None);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();

        List<MetricMeasurement> failureMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.hangfire.job.failures")];

        failureMetrics.ShouldHaveSingleItem();
        failureMetrics[0].Value.ShouldBe(1);
        failureMetrics[0].Tags["job.type"].ShouldBe("FailingJob");
        failureMetrics[0].Tags["error.type"].ShouldBe("InvalidOperationException");
    }

    [Fact(DisplayName = "Should record duration histogram with success result tag when job succeeds")]
    public async Task ExecuteJobAsync_ShouldRecordDurationHistogram_WhenJobSucceedsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new SuccessfulJob();

        // Act
        await scheduler.ExecuteJobAsync(job, "SuccessfulJob", CancellationToken.None);

        // Assert
        List<MetricMeasurement> durationMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.hangfire.job.duration")];

        durationMetrics.ShouldHaveSingleItem();
        durationMetrics[0].Value.ShouldBeGreaterThanOrEqualTo(0);
        durationMetrics[0].Tags["job.type"].ShouldBe("SuccessfulJob");
        durationMetrics[0].Tags["result"].ShouldBe("success");
    }

    [Fact(DisplayName = "Should record duration histogram with failure result and error type when job fails")]
    public async Task ExecuteJobAsync_ShouldRecordDurationHistogram_WhenJobFailsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new FailingJob();

        // Act
        Func<Task> act = async () => await scheduler.ExecuteJobAsync(job, "FailingJob", CancellationToken.None);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();

        List<MetricMeasurement> durationMetrics = [.. _recordedMetrics.Where(m => m.InstrumentName == "neba.hangfire.job.duration")];

        durationMetrics.ShouldHaveSingleItem();
        durationMetrics[0].Value.ShouldBeGreaterThanOrEqualTo(0);
        durationMetrics[0].Tags["job.type"].ShouldBe("FailingJob");
        durationMetrics[0].Tags["result"].ShouldBe("failure");
        durationMetrics[0].Tags["error.type"].ShouldBe("InvalidOperationException");
    }

    [Fact(DisplayName = "Should record all expected metrics when job succeeds")]
    public async Task ExecuteJobAsync_ShouldRecordAllExpectedMetrics_WhenJobSucceedsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new SuccessfulJob();

        // Act
        await scheduler.ExecuteJobAsync(job, "SuccessfulJob", CancellationToken.None);

        // Assert
        _recordedMetrics.Select(m => m.InstrumentName).Distinct().ShouldBe(
            [
                "neba.hangfire.job.executions",
                "neba.hangfire.job.successes",
                "neba.hangfire.job.duration"
            ],
            ignoreOrder: true);
    }

    [Fact(DisplayName = "Should record all expected metrics when job fails")]
    public async Task ExecuteJobAsync_ShouldRecordAllExpectedMetrics_WhenJobFailsAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new FailingJob();

        // Act
        Func<Task> act = async () => await scheduler.ExecuteJobAsync(job, "FailingJob", CancellationToken.None);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();

        _recordedMetrics.Select(m => m.InstrumentName).Distinct().ShouldBe(
            [
                "neba.hangfire.job.executions",
                "neba.hangfire.job.failures",
                "neba.hangfire.job.duration"
            ],
            ignoreOrder: true);
    }

    [Fact(DisplayName = "Should have activity duration matching metric duration approximately")]
    public async Task ExecuteJobAsync_ShouldHaveMatchingDuration_WhenActivityAndMetricRecordedAsync()
    {
        // Arrange
        _recordedActivities.Clear();
        _recordedMetrics.Clear();
        HangfireBackgroundJobScheduler scheduler = CreateScheduler();
        var job = new SuccessfulJob();

        // Act
        await scheduler.ExecuteJobAsync(job, "SuccessfulJob", CancellationToken.None);

        // Assert
        Activity activity = GetActivityForJob("SuccessfulJob");

        double activityDuration = Convert.ToDouble(activity.GetTagItem("job.duration_ms"), System.Globalization.CultureInfo.InvariantCulture);

        MetricMeasurement durationMetric = _recordedMetrics
            .Single(m => m.InstrumentName == "neba.hangfire.job.duration");

        // Allow for small timing differences (within 5ms)
        Math.Abs(activityDuration - durationMetric.Value).ShouldBeLessThan(5.0);
    }
}