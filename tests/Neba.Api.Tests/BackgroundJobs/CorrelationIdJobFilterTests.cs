using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.States;
using Hangfire.Storage;

using Neba.Api.Auditing;
using Neba.Api.BackgroundJobs;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("Infrastructure.BackgroundJobs")]
public sealed class CorrelationIdJobFilterTests
{
    [Fact(DisplayName = "OnCreating should capture a new correlation id as a job parameter when none is ambient")]
    public void OnCreating_ShouldSetJobParameter_WhenNoAmbientCorrelationId()
    {
        // Arrange
        var filter = new CorrelationIdJobFilter();
        using var scope = new TestCreatingContextScope();

        // Act
        filter.OnCreating(scope.Context);

        // Assert
        scope.Context.GetJobParameter<string>("CorrelationId").ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "OnCreating should reuse the ambient correlation id when one is already set")]
    public void OnCreating_ShouldReuseAmbientCorrelationId_WhenOneIsSet()
    {
        // Arrange
        var filter = new CorrelationIdJobFilter();
        using var scope = new TestCreatingContextScope();

        // Act
        using (AmbientCorrelationContext.SetCorrelationId("parent-correlation-id"))
        {
            filter.OnCreating(scope.Context);
        }

        // Assert
        scope.Context.GetJobParameter<string>("CorrelationId").ShouldBe("parent-correlation-id");
    }

    [Fact(DisplayName = "OnPerforming/OnPerformed should set and then clear the ambient correlation id")]
    public void OnPerformingThenOnPerformed_ShouldSetThenClearAmbientCorrelationId()
    {
        // Arrange
        var filter = new CorrelationIdJobFilter();
        using var scope = new TestPerformContextScope();
        scope.Context.Connection.SetJobParameter(scope.Context.BackgroundJob.Id, "CorrelationId", "\"job-correlation-id\"");

        // Act
        filter.OnPerforming(new Hangfire.Server.PerformingContext(scope.Context));

        // Assert
        AmbientCorrelationContext.CorrelationId.ShouldBe("job-correlation-id");

        // Act
        filter.OnPerformed(new Hangfire.Server.PerformedContext(scope.Context, null, canceled: false, null));

        // Assert
        AmbientCorrelationContext.CorrelationId.ShouldBeNull();
    }

    [Fact(DisplayName = "OnPerforming should not set an ambient correlation id when no job parameter exists")]
    public void OnPerforming_ShouldNotSetAmbientCorrelationId_WhenNoJobParameterExists()
    {
        // Arrange
        var filter = new CorrelationIdJobFilter();
        using var scope = new TestPerformContextScope();

        // Act
        filter.OnPerforming(new Hangfire.Server.PerformingContext(scope.Context));

        // Assert
        AmbientCorrelationContext.CorrelationId.ShouldBeNull();
    }

    /// <summary>
    /// Bundles a real, InMemoryStorage-backed CreatingContext, mirroring TestPerformContextScope's
    /// rationale for PerformContext - CreateContext's public constructor
    /// (JobStorage, IStorageConnection, Job, IState) makes this possible without a full Hangfire
    /// client pipeline.
    /// </summary>
    private sealed class TestCreatingContextScope : IDisposable
    {
        private readonly InMemoryStorage _storage;
        private readonly IStorageConnection _connection;

        public CreatingContext Context { get; }

        public TestCreatingContextScope()
        {
            _storage = new InMemoryStorage();
            _connection = _storage.GetConnection();
            var job = Job.FromExpression(() => Console.WriteLine("noop"));

            Context = new CreatingContext(new CreateContext(_storage, _connection, job, new EnqueuedState()));
        }

        public void Dispose()
        {
            _connection.Dispose();
            _storage.Dispose();
        }
    }
}
