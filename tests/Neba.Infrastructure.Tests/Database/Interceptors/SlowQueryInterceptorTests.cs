using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

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

    private static SlowQueryInterceptor CreateInterceptor(Mock<ILogger<SlowQueryInterceptor>> logger) =>
        new(logger.Object, new SlowQueryOptions { ThresholdMs = ThresholdMs });

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

    private static void SetupLogWarning(Mock<ILogger<SlowQueryInterceptor>> logger)
    {
        logger
            .Setup(l => l.IsEnabled(LogLevel.Warning))
            .Returns(true);
        logger
            .Setup(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    private static void VerifyLoggedOnce(Mock<ILogger<SlowQueryInterceptor>> logger) =>
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once());

    // ── Threshold logic (via ReaderExecuted) ─────────────────────────────────

    [Fact(DisplayName = "Logs warning when duration exceeds threshold")]
    public void ReaderExecuted_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new Mock<ILogger<SlowQueryInterceptor>>(MockBehavior.Strict);
        SetupLogWarning(logger);

        CreateInterceptor(logger).ReaderExecuted(
            CreateCommand(), MakeEventData(AboveThreshold), null!);

        VerifyLoggedOnce(logger);
    }

    [Fact(DisplayName = "Does not log when duration is below threshold")]
    public void ReaderExecuted_WhenDurationBelowThreshold_DoesNotLog()
    {
        var logger = new Mock<ILogger<SlowQueryInterceptor>>(MockBehavior.Strict);

        CreateInterceptor(logger).ReaderExecuted(
            CreateCommand(), MakeEventData(BelowThreshold), null!);

        logger.Verify(
            l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never());
    }

    [Fact(DisplayName = "Logs warning when duration equals threshold exactly")]
    public void ReaderExecuted_WhenDurationEqualsThreshold_LogsWarning()
    {
        var logger = new Mock<ILogger<SlowQueryInterceptor>>(MockBehavior.Strict);
        SetupLogWarning(logger);

        CreateInterceptor(logger).ReaderExecuted(
            CreateCommand(), MakeEventData(AtThreshold), null!);

        VerifyLoggedOnce(logger);
    }

    // ── Each hook delegates to LogIfSlow ─────────────────────────────────────

    [Fact(DisplayName = "ReaderExecutedAsync logs warning when duration exceeds threshold")]
    public async Task ReaderExecutedAsync_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new Mock<ILogger<SlowQueryInterceptor>>(MockBehavior.Strict);
        SetupLogWarning(logger);

        await CreateInterceptor(logger).ReaderExecutedAsync(
            CreateCommand(), MakeEventData(AboveThreshold), null!, CancellationToken.None);

        VerifyLoggedOnce(logger);
    }

    [Fact(DisplayName = "NonQueryExecuted logs warning when duration exceeds threshold")]
    public void NonQueryExecuted_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new Mock<ILogger<SlowQueryInterceptor>>(MockBehavior.Strict);
        SetupLogWarning(logger);

        CreateInterceptor(logger).NonQueryExecuted(
            CreateCommand("DELETE FROM x WHERE 1=0"), MakeEventData(AboveThreshold), 0);

        VerifyLoggedOnce(logger);
    }

    [Fact(DisplayName = "NonQueryExecutedAsync logs warning when duration exceeds threshold")]
    public async Task NonQueryExecutedAsync_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new Mock<ILogger<SlowQueryInterceptor>>(MockBehavior.Strict);
        SetupLogWarning(logger);

        await CreateInterceptor(logger).NonQueryExecutedAsync(
            CreateCommand("DELETE FROM x WHERE 1=0"), MakeEventData(AboveThreshold), 0, CancellationToken.None);

        VerifyLoggedOnce(logger);
    }

    [Fact(DisplayName = "ScalarExecuted logs warning when duration exceeds threshold")]
    public void ScalarExecuted_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new Mock<ILogger<SlowQueryInterceptor>>(MockBehavior.Strict);
        SetupLogWarning(logger);

        CreateInterceptor(logger).ScalarExecuted(
            CreateCommand("SELECT COUNT(*)"), MakeEventData(AboveThreshold), null);

        VerifyLoggedOnce(logger);
    }

    [Fact(DisplayName = "ScalarExecutedAsync logs warning when duration exceeds threshold")]
    public async Task ScalarExecutedAsync_WhenDurationExceedsThreshold_LogsWarning()
    {
        var logger = new Mock<ILogger<SlowQueryInterceptor>>(MockBehavior.Strict);
        SetupLogWarning(logger);

        await CreateInterceptor(logger).ScalarExecutedAsync(
            CreateCommand("SELECT COUNT(*)"), MakeEventData(AboveThreshold), null, CancellationToken.None);

        VerifyLoggedOnce(logger);
    }
}
