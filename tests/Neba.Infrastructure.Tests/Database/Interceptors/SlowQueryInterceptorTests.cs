using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

using Neba.Infrastructure.Database.Interceptors;
using Neba.Infrastructure.Database.Options;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Database.Interceptors;

[UnitTest]
[Component("Infrastructure.Database")]
public sealed class SlowQueryInterceptorTests
{
    private const int ThresholdMs = 500;
    private static readonly TimeSpan BelowThreshold = TimeSpan.FromMilliseconds(ThresholdMs - 1);
    private static readonly TimeSpan AtThreshold = TimeSpan.FromMilliseconds(ThresholdMs);
    private static readonly TimeSpan AboveThreshold = TimeSpan.FromMilliseconds(ThresholdMs + 1);

    private static SlowQueryInterceptor CreateInterceptor(ILogger<SlowQueryInterceptor> logger) =>
        new(logger, new SlowQueryOptions { ThresholdMs = ThresholdMs });

    private static DbCommand CreateCommand(string sql = "SELECT 1")
    {
        var cmd = new Mock<DbCommand>(MockBehavior.Loose);
        cmd.SetupGet(c => c.CommandText).Returns(sql);
        return cmd.Object;
    }

    private static CommandExecutedEventData MakeEventData(TimeSpan duration)
    {
        var loggingOptions = new Mock<ILoggingOptions>(MockBehavior.Loose);
        var eventDef = new Mock<EventDefinitionBase>(
            MockBehavior.Loose,
            loggingOptions.Object,
            new EventId(1),
            LogLevel.None,
            "test");
        var connection = new Mock<DbConnection>(MockBehavior.Loose);
        var command = new Mock<DbCommand>(MockBehavior.Loose);

        return new CommandExecutedEventData(
            eventDef.Object,
            (_, _) => string.Empty,
            connection.Object,
            command.Object,
            string.Empty,
            null,
            DbCommandMethod.ExecuteReader,
            Guid.Empty,
            Guid.Empty,
            null,
            false,
            false,
            DateTimeOffset.UtcNow,
            duration,
            CommandSource.LinqQuery);
    }

    // ── Threshold logic (via ReaderExecuted) ─────────────────────────────────

    [Fact(DisplayName = "Logs warning when duration exceeds threshold")]
    public void ReaderExecuted_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        CreateInterceptor(logger).ReaderExecuted(
            CreateCommand(), MakeEventData(AboveThreshold), null!);

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "Does not log when duration is below threshold")]
    public void ReaderExecuted_WhenDurationBelowThreshold_DoesNotLog()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        CreateInterceptor(logger).ReaderExecuted(
            CreateCommand(), MakeEventData(BelowThreshold), null!);

        logger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Logs warning when duration equals threshold exactly")]
    public void ReaderExecuted_WhenDurationEqualsThreshold_LogsWarning()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        CreateInterceptor(logger).ReaderExecuted(
            CreateCommand(), MakeEventData(AtThreshold), null!);

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    // ── Each hook delegates to LogIfSlow ─────────────────────────────────────

    [Fact(DisplayName = "ReaderExecutedAsync logs warning when duration exceeds threshold")]
    public async Task ReaderExecutedAsync_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        await CreateInterceptor(logger).ReaderExecutedAsync(
            CreateCommand(), MakeEventData(AboveThreshold), null!, CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "NonQueryExecuted logs warning when duration exceeds threshold")]
    public void NonQueryExecuted_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        CreateInterceptor(logger).NonQueryExecuted(
            CreateCommand("DELETE FROM x WHERE 1=0"), MakeEventData(AboveThreshold), 0);

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "NonQueryExecutedAsync logs warning when duration exceeds threshold")]
    public async Task NonQueryExecutedAsync_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        await CreateInterceptor(logger).NonQueryExecutedAsync(
            CreateCommand("DELETE FROM x WHERE 1=0"), MakeEventData(AboveThreshold), 0, CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "ScalarExecuted logs warning when duration exceeds threshold")]
    public void ScalarExecuted_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        CreateInterceptor(logger).ScalarExecuted(
            CreateCommand("SELECT COUNT(*)"), MakeEventData(AboveThreshold), null);

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "ScalarExecutedAsync logs warning when duration exceeds threshold")]
    public async Task ScalarExecutedAsync_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();

        await CreateInterceptor(logger).ScalarExecutedAsync(
            CreateCommand("SELECT COUNT(*)"), MakeEventData(AboveThreshold), null, CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    // ── Truncation ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Truncates query text exceeding 2000 characters in the log message")]
    public void ReaderExecuted_WhenCommandTextExceedsLimit_TruncatesInLog()
    {
        var logger = new FakeLogger<SlowQueryInterceptor>();
        var longSql = "SELECT 1 FROM x WHERE id IN (" + new string('x', 2100) + ")";

        CreateInterceptor(logger).ReaderExecuted(
            CreateCommand(longSql), MakeEventData(AboveThreshold), null!);

        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        var commandText = record.GetStructuredStateValue("CommandText")?.ToString();

        commandText.ShouldNotBeNull();
        commandText.ShouldContain($"... [{longSql.Length - 2000} chars truncated]");
        commandText.Length.ShouldBeLessThan(longSql.Length);
    }
}