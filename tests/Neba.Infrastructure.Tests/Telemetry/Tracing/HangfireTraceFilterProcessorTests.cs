using System.Diagnostics;

using Neba.ServiceDefaults.Telemetry;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Telemetry.Tracing;

#pragma warning disable CA2000 // Dispose objects before losing scope - processors don't need disposal in tests

[UnitTest]
[Component("ServiceDefaults.Telemetry.Tracing")]
public sealed class HangfireTraceFilterProcessorTests
{
    #region Connection Validation Queries

    [Fact(DisplayName = "Should filter SELECT 1 connection validation query")]
    public void OnEnd_ShouldFilterTrace_WhenConnectionValidationQuery()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("SELECT 1;");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    #endregion

    #region Hangfire Job Operations (Should NOT Filter)

    [Fact(DisplayName = "Should keep INSERT into job table")]
    public void OnEnd_ShouldKeepTrace_WhenInsertIntoJobTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("INSERT INTO \"hangfire\".\"job\" VALUES (@param)");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    [Fact(DisplayName = "Should keep UPDATE job table")]
    public void OnEnd_ShouldKeepTrace_WhenUpdateJobTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("UPDATE \"hangfire\".\"job\" SET state = 'Completed'");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    [Fact(DisplayName = "Should keep SELECT from job table")]
    public void OnEnd_ShouldKeepTrace_WhenSelectFromJobTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("SELECT * FROM \"hangfire\".\"job\" WHERE id = @id");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    [Fact(DisplayName = "Should keep INSERT into jobparameter table")]
    public void OnEnd_ShouldKeepTrace_WhenInsertIntoJobParameterTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("INSERT INTO \"hangfire\".\"jobparameter\" VALUES (@param)");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    [Fact(DisplayName = "Should keep INSERT into jobqueue")]
    public void OnEnd_ShouldKeepTrace_WhenEnqueueingJob()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("INSERT INTO \"hangfire\".\"jobqueue\" (\"queue\", \"jobid\") VALUES ('default', 123)");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    #endregion

    #region Hangfire Internal Housekeeping (Should Filter)

    [Fact(DisplayName = "Should filter INSERT into lock table")]
    public void OnEnd_ShouldFilterTrace_WhenInsertIntoLockTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("INSERT INTO \"hangfire\".\"lock\"(\"resource\", \"acquired\") SELECT @Resource, @Acquired");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should filter DELETE from lock table")]
    public void OnEnd_ShouldFilterTrace_WhenDeleteFromLockTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("DELETE FROM \"hangfire\".\"lock\" WHERE \"resource\" = @Resource");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should filter SELECT from jobqueue (polling)")]
    public void OnEnd_ShouldFilterTrace_WhenPollingJobQueue()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("SELECT * FROM \"hangfire\".\"jobqueue\" WHERE \"queue\" = 'default'");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should filter UPDATE jobqueue fetchedat")]
    public void OnEnd_ShouldFilterTrace_WhenUpdatingJobQueueFetch()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("UPDATE \"hangfire\".\"jobqueue\" SET \"fetchedat\" = NOW() WHERE \"id\" = 123");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should filter DELETE from set table")]
    public void OnEnd_ShouldFilterTrace_WhenCleaningSetTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("DELETE FROM \"hangfire\".\"set\" WHERE \"expireat\" < NOW()");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should filter complex set query with BETWEEN")]
    public void OnEnd_ShouldFilterTrace_WhenComplexSetQuery()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        const string queryText = """
          SELECT "value" 
          FROM "hangfire"."set" 
          WHERE "key" = @Key 
          AND "score" BETWEEN @FromScore AND @ToScore 
          ORDER BY "score" LIMIT @Limit;
        """;
        using var activity = CreatePostgreSqlActivity(queryText);

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should filter DELETE from hash table")]
    public void OnEnd_ShouldFilterTrace_WhenCleaningHashTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("DELETE FROM \"hangfire\".\"hash\" WHERE \"expireat\" < NOW()");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should filter server table queries")]
    public void OnEnd_ShouldFilterTrace_WhenQueryingServerTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("SELECT * FROM \"hangfire\".\"server\"");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should filter state table queries")]
    public void OnEnd_ShouldFilterTrace_WhenQueryingStateTable()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("INSERT INTO \"hangfire\".\"state\" VALUES (@jobid, @state)");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    #endregion

    #region Non-Hangfire Queries (Should NOT Filter)

    [Fact(DisplayName = "Should keep public schema queries")]
    public void OnEnd_ShouldKeepTrace_WhenPublicSchemaQuery()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("SELECT * FROM \"public\".\"users\"");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    [Fact(DisplayName = "Should keep business table queries")]
    public void OnEnd_ShouldKeepTrace_WhenBusinessTableQuery()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activity = CreatePostgreSqlActivity("INSERT INTO tournaments (name) VALUES ('Test')");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    #endregion

    #region Application-Level Hangfire Traces (from ActivitySource)

    [Fact(DisplayName = "Should keep Hangfire ExecuteJob trace")]
    public void OnEnd_ShouldKeepTrace_WhenHangfireExecuteJob()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activitySource = new ActivitySource("Hangfire.Core");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("ExecuteJob");
        activity.ShouldNotBeNull();

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    [Fact(DisplayName = "Should filter Hangfire PollingWorker trace")]
    public void OnEnd_ShouldFilterTrace_WhenHangfirePollingWorker()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activitySource = new ActivitySource("Hangfire.Core");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("PollingWorker");
        activity.ShouldNotBeNull();

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertFiltered(activity);
    }

    [Fact(DisplayName = "Should keep Hangfire trace with error")]
    public void OnEnd_ShouldKeepTrace_WhenHangfireActivityHasError()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activitySource = new ActivitySource("Hangfire.Core");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("PollingWorker");
        activity.ShouldNotBeNull();
        activity.SetStatus(ActivityStatusCode.Error, "Test error");

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    #endregion

    #region Edge Cases

    [Fact(DisplayName = "Should keep trace when not PostgreSQL client")]
    public void OnEnd_ShouldKeepTrace_WhenNotPostgreSqlClient()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activitySource = new ActivitySource("MyApp");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("MyOperation", ActivityKind.Server);
        activity.ShouldNotBeNull();

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    [Fact(DisplayName = "Should keep trace when query text tag is missing")]
    public void OnEnd_ShouldKeepTrace_WhenQueryTextMissing()
    {
        // Arrange
        var processor = new HangfireTraceFilterProcessor();
        using var activitySource = new ActivitySource("Npgsql");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = activitySource.StartActivity("postgresql", ActivityKind.Client);
        activity.ShouldNotBeNull();

        // Act
        processor.OnEnd(activity);

        // Assert
        AssertNotFiltered(activity);
    }

    #endregion

    #region Helper Methods

    private static Activity CreatePostgreSqlActivity(string queryText)
    {
        using var activitySource = new ActivitySource("Npgsql");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var activity = activitySource.StartActivity("postgresql", ActivityKind.Client);
        activity.ShouldNotBeNull();
        activity.SetTag("db.query.text", queryText);
        activity.SetTag("db.system.name", "postgresql");
        return activity;
    }

    private static void AssertFiltered(Activity activity)
    {
        (activity.ActivityTraceFlags & ActivityTraceFlags.Recorded).ShouldBe(ActivityTraceFlags.None);
    }

    private static void AssertNotFiltered(Activity activity)
    {
        (activity.ActivityTraceFlags & ActivityTraceFlags.Recorded).ShouldNotBe(ActivityTraceFlags.None);
    }

    #endregion
}