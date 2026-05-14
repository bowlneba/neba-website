using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

using Neba.Application.BackgroundJobs;
using Neba.Domain;
using Neba.Api.Database.Interceptors;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Interceptors;

[UnitTest]
[Component("Infrastructure.Database")]
public sealed class DomainEventDispatcherInterceptorTests
{
    // ── Test doubles ─────────────────────────────────────────────────────────

    private sealed class TestDomainEvent : IDomainEvent;
    private sealed class AnotherDomainEvent : IDomainEvent;

    private sealed class TestAggregate : AggregateRoot
    {
        public int Id { get; init; }
        public void Raise(IDomainEvent evt) => AddDomainEvent(evt);
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TestAggregate>().HasKey(a => a.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .EnableServiceProviderCaching(false)
            .Options;
        return new TestDbContext(options);
    }

    private static DomainEventDispatcherInterceptor CreateInterceptor(Mock<IBackgroundJobClient> client) =>
        new(client.Object);

    private static SaveChangesCompletedEventData CreateEventData(DbContext? context)
    {
        var loggingOptions = new Mock<ILoggingOptions>(MockBehavior.Loose);
        var eventDef = new Mock<EventDefinitionBase>(
            MockBehavior.Loose,
            loggingOptions.Object,
            new EventId(1),
            LogLevel.None,
            "test");
        return new SaveChangesCompletedEventData(eventDef.Object, (_, _) => string.Empty, context!, 0);
    }

    // ── Null context guard ────────────────────────────────────────────────────

    [Fact(DisplayName = "Does not create any jobs when context is null")]
    public void SavedChanges_WhenContextIsNull_DoesNotCreateAnyJobs()
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var interceptor = CreateInterceptor(client);

        // Strict mock: any call without a setup throws immediately — no job was created.
        // Assert the pass-through return value to satisfy static analysis.
        interceptor.SavedChanges(CreateEventData(null), 42).ShouldBe(42);
    }

    // ── Change tracker filtering ──────────────────────────────────────────────

    [Fact(DisplayName = "Does not create any jobs when no aggregates are tracked")]
    public void SavedChanges_WhenNoAggregatesTracked_DoesNotCreateAnyJobs()
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        using var ctx = CreateContext();

        CreateInterceptor(client).SavedChanges(CreateEventData(ctx), 0);

        client.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Does not create any jobs when tracked aggregate has no domain events")]
    public void SavedChanges_WhenAggregateHasNoDomainEvents_DoesNotCreateAnyJobs()
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        using var ctx = CreateContext();
        ctx.Entry(new TestAggregate { Id = 1 }).State = EntityState.Unchanged;

        CreateInterceptor(client).SavedChanges(CreateEventData(ctx), 0);

        client.VerifyNoOtherCalls();
    }

    // ── Dispatching ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Creates one job when aggregate has one domain event (sync)")]
    public void SavedChanges_WithOneDomainEvent_CreatesOneJob()
    {
        var callCount = 0;
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
              .Callback((Job _, IState _) => callCount++)
              .Returns("job-1");

        using var ctx = CreateContext();
        var aggregate = new TestAggregate { Id = 1 };
        aggregate.Raise(new TestDomainEvent());
        ctx.Entry(aggregate).State = EntityState.Unchanged;

        CreateInterceptor(client).SavedChanges(CreateEventData(ctx), 0);

        callCount.ShouldBe(1);
    }

    [Fact(DisplayName = "Creates one job when aggregate has one domain event (async)")]
    public async Task SavedChangesAsync_WithOneDomainEvent_CreatesOneJob()
    {
        var callCount = 0;
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
              .Callback((Job _, IState _) => callCount++)
              .Returns("job-1");

        await using var ctx = CreateContext();
        var aggregate = new TestAggregate { Id = 1 };
        aggregate.Raise(new TestDomainEvent());
        ctx.Entry(aggregate).State = EntityState.Unchanged;

        await CreateInterceptor(client).SavedChangesAsync(CreateEventData(ctx), 0, CancellationToken.None);

        callCount.ShouldBe(1);
    }

    [Fact(DisplayName = "Creates one job per event when aggregate raises multiple domain events")]
    public void SavedChanges_WithMultipleEventsOnOneAggregate_CreatesOneJobPerEvent()
    {
        var callCount = 0;
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
              .Callback((Job _, IState _) => callCount++)
              .Returns("job-id");

        using var ctx = CreateContext();
        var aggregate = new TestAggregate { Id = 1 };
        aggregate.Raise(new TestDomainEvent());
        aggregate.Raise(new TestDomainEvent());
        aggregate.Raise(new TestDomainEvent());
        ctx.Entry(aggregate).State = EntityState.Unchanged;

        CreateInterceptor(client).SavedChanges(CreateEventData(ctx), 0);

        callCount.ShouldBe(3);
    }

    [Fact(DisplayName = "Creates jobs for all events across multiple aggregates")]
    public void SavedChanges_WithMultipleAggregates_CreatesJobsForAllEvents()
    {
        var callCount = 0;
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
              .Callback((Job _, IState _) => callCount++)
              .Returns("job-id");

        using var ctx = CreateContext();

        var first = new TestAggregate { Id = 1 };
        first.Raise(new TestDomainEvent());
        ctx.Entry(first).State = EntityState.Unchanged;

        var second = new TestAggregate { Id = 2 };
        second.Raise(new AnotherDomainEvent());
        second.Raise(new AnotherDomainEvent());
        ctx.Entry(second).State = EntityState.Unchanged;

        CreateInterceptor(client).SavedChanges(CreateEventData(ctx), 0);

        callCount.ShouldBe(3);
    }

    // ── Event clearing ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Clears domain events from aggregate after dispatch")]
    public void SavedChanges_AfterDispatch_ClearsDomainEventsFromAggregate()
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-1");

        using var ctx = CreateContext();
        var aggregate = new TestAggregate { Id = 1 };
        aggregate.Raise(new TestDomainEvent());
        ctx.Entry(aggregate).State = EntityState.Unchanged;

        CreateInterceptor(client).SavedChanges(CreateEventData(ctx), 0);

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    // ── Job shape ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Creates job targeting IDomainEventJob<TEvent> with HandleAsync method")]
    public void SavedChanges_CreatesJobTargetingCorrectHandlerType()
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        Job? capturedJob = null;
        client
            .Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((j, _) => capturedJob = j)
            .Returns("job-1");

        using var ctx = CreateContext();
        var aggregate = new TestAggregate { Id = 1 };
        aggregate.Raise(new TestDomainEvent());
        ctx.Entry(aggregate).State = EntityState.Unchanged;

        CreateInterceptor(client).SavedChanges(CreateEventData(ctx), 0);

        capturedJob!.Type.ShouldBe(typeof(IDomainEventJob<TestDomainEvent>));
        capturedJob.Method.Name.ShouldBe("HandleAsync");
    }

    [Fact(DisplayName = "Creates job with EnqueuedState")]
    public void SavedChanges_CreatesJobWithEnqueuedState()
    {
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        IState? capturedState = null;
        client
            .Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((_, s) => capturedState = s)
            .Returns("job-1");

        using var ctx = CreateContext();
        var aggregate = new TestAggregate { Id = 1 };
        aggregate.Raise(new TestDomainEvent());
        ctx.Entry(aggregate).State = EntityState.Unchanged;

        CreateInterceptor(client).SavedChanges(CreateEventData(ctx), 0);

        capturedState.ShouldBeOfType<EnqueuedState>();
    }
}