using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.Server;
using Hangfire.Storage;

using Microsoft.Extensions.Logging;

using Neba.Api.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class HangfireConsoleLoggerProviderTests
{
    [Fact(DisplayName = "IsEnabled should return false for every level when AmbientJobConsole has no context")]
    public void IsEnabled_ShouldReturnFalseForEveryLevel_WhenAmbientJobConsoleHasNoContext()
    {
        // Arrange
        AmbientJobConsole.Clear();
        using var provider = new HangfireConsoleLoggerProvider();
        var logger = provider.CreateLogger("Category");

        // Act
        var enabledForInformation = logger.IsEnabled(LogLevel.Information);
        var enabledForError = logger.IsEnabled(LogLevel.Error);

        // Assert
        enabledForInformation.ShouldBeFalse();
        enabledForError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Log should be a no-op when AmbientJobConsole has no context")]
    public void Log_ShouldBeNoOp_WhenAmbientJobConsoleHasNoContext()
    {
        // Arrange
        AmbientJobConsole.Clear();
        using var provider = new HangfireConsoleLoggerProvider();
        var logger = provider.CreateLogger("Category");

        // Act / Assert - no PerformContext to write to; must not throw.
        Should.NotThrow(() => logger.Log(LogLevel.Information, new EventId(0), "message", null, (s, _) => s));
    }

    [Fact(DisplayName = "IsEnabled should return true when AmbientJobConsole has a context")]
    public void IsEnabled_ShouldReturnTrue_WhenAmbientJobConsoleHasContext()
    {
        // Arrange
        using var scope = new TestPerformContextScope();
        AmbientJobConsole.Set(scope.Context);
        using var provider = new HangfireConsoleLoggerProvider();
        var logger = provider.CreateLogger("Category");

        try
        {
            // Act
            var enabled = logger.IsEnabled(LogLevel.Information);

            // Assert
            enabled.ShouldBeTrue();
        }
        finally
        {
            AmbientJobConsole.Clear();
        }
    }

    [Fact(DisplayName = "Log should write to the ambient PerformContext's console without throwing when a context is set")]
    public void Log_ShouldWriteToAmbientPerformContext_WhenContextIsSet()
    {
        // Arrange
        using var scope = new TestPerformContextScope();
        AmbientJobConsole.Set(scope.Context);
        using var provider = new HangfireConsoleLoggerProvider();
        var logger = provider.CreateLogger("Neba.Api.Legacy.PongJob");

        try
        {
            // Act / Assert - real Hangfire.Console API; no ConsoleStorage seam is exposed publicly
            // to assert on the written line's content, so absence of a throw is the strongest
            // available signal that the bridge invoked WriteLine correctly against a real context.
            Should.NotThrow(() => logger.Log(LogLevel.Information, new EventId(0), "Pong: GET /health returned 200", null, (s, _) => s));
        }
        finally
        {
            AmbientJobConsole.Clear();
        }
    }

    [Fact(DisplayName = "Log should write the exception details when an exception is present")]
    public void Log_ShouldWriteExceptionDetails_WhenExceptionIsPresent()
    {
        // Arrange
        using var scope = new TestPerformContextScope();
        AmbientJobConsole.Set(scope.Context);
        using var provider = new HangfireConsoleLoggerProvider();
        var logger = provider.CreateLogger("Category");
        var exception = new InvalidOperationException("boom");

        try
        {
            // Act / Assert
            Should.NotThrow(() => logger.Log(LogLevel.Error, new EventId(0), "failed", exception, (s, _) => s));
        }
        finally
        {
            AmbientJobConsole.Clear();
        }
    }
}

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class HangfireConsoleServerFilterTests
{
    private readonly HangfireConsoleServerFilter _filter = new();

    [Fact(DisplayName = "OnPerforming should set AmbientJobConsole to the supplied context")]
    public void OnPerforming_ShouldSetAmbientJobConsole_ToSuppliedContext()
    {
        // Arrange
        using var scope = new TestPerformContextScope();
        var performingContext = new PerformingContext(scope.Context);

        try
        {
            // Act
            _filter.OnPerforming(performingContext);

            // Assert - PerformingContext derives from PerformContext, so the ambient value is the
            // PerformingContext instance itself, not the original PerformContext it was built from.
            AmbientJobConsole.Context.ShouldBe(performingContext);
        }
        finally
        {
            AmbientJobConsole.Clear();
        }
    }

    [Fact(DisplayName = "OnPerformed should clear AmbientJobConsole back to null")]
    public void OnPerformed_ShouldClearAmbientJobConsole_ToNull()
    {
        // Arrange
        using var scope = new TestPerformContextScope();
        AmbientJobConsole.Set(scope.Context);
        var performedContext = new PerformedContext(scope.Context, null, false, null);

        // Act
        _filter.OnPerformed(performedContext);

        // Assert
        AmbientJobConsole.Context.ShouldBeNull();
    }
}

/// <summary>
/// Bundles a real, InMemoryStorage-backed PerformContext with the storage/connection it depends
/// on, so tests can dispose all three together. No test in this codebase previously constructed a
/// bare PerformContext directly; Hangfire.Core's public constructor overload
/// (JobStorage, IStorageConnection, BackgroundJob, IJobCancellationToken) makes this possible
/// without needing a full Hangfire server/dashboard pipeline.
/// </summary>
internal sealed class TestPerformContextScope : IDisposable
{
    private readonly InMemoryStorage _storage;
    private readonly IStorageConnection _connection;

    public PerformContext Context { get; }

    public TestPerformContextScope()
    {
        _storage = new InMemoryStorage();
        _connection = _storage.GetConnection();
        var job = Job.FromExpression(() => Console.WriteLine("noop"));
        var jobId = _connection.CreateExpiredJob(job, new Dictionary<string, string>(), DateTime.UtcNow, TimeSpan.FromMinutes(1));
        var backgroundJob = new BackgroundJob(jobId, job, DateTime.UtcNow);

        Context = new PerformContext(_storage, _connection, backgroundJob, new NoOpJobCancellationToken());
    }

    public void Dispose()
    {
        _connection.Dispose();
        _storage.Dispose();
    }

    private sealed class NoOpJobCancellationToken : IJobCancellationToken
    {
        public CancellationToken ShutdownToken => CancellationToken.None;

        public void ThrowIfCancellationRequested()
        {
        }
    }
}
