using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database.Interceptors;
using Neba.Api.Domain;
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
        new(new DomainEventPublisher(client.Object));

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

    // ── Strict publisher: who gets published ──────────────────────────────────

    private static TestAggregate AggregateWithEvents(int id, int eventCount)
    {
        var aggregate = new TestAggregate { Id = id };
        for (var i = 0; i < eventCount; i++)
        {
            aggregate.Raise(new TestDomainEvent());
        }

        return aggregate;
    }

    [Fact(DisplayName = "SavedChanges should call PublishAndClear once per tracked aggregate that has events, and not for aggregates without")]
    public void SavedChanges_WithMixedAggregates_CallsPublishAndClearOncePerAggregateWithEvents()
    {
        // Arrange
        var first = AggregateWithEvents(1, 1);
        var second = AggregateWithEvents(2, 2);
        var withoutEvents = AggregateWithEvents(3, 0);
        var publisher = new Mock<IDomainEventPublisher>(MockBehavior.Strict);
        publisher.Setup(p => p.PublishAndClear(first));
        publisher.Setup(p => p.PublishAndClear(second));

        using var ctx = CreateContext();
        ctx.Entry(first).State = EntityState.Unchanged;
        ctx.Entry(second).State = EntityState.Unchanged;
        ctx.Entry(withoutEvents).State = EntityState.Unchanged;
        var interceptor = new DomainEventDispatcherInterceptor(publisher.Object);

        // Act
        var result = interceptor.SavedChanges(CreateEventData(ctx), 7);

        // Assert
        result.ShouldBe(7);
        publisher.Verify(p => p.PublishAndClear(first), Times.Once);
        publisher.Verify(p => p.PublishAndClear(second), Times.Once);
        publisher.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "SavedChangesAsync should call PublishAndClear once per tracked aggregate that has events, and not for aggregates without")]
    public async Task SavedChangesAsync_WithMixedAggregates_CallsPublishAndClearOncePerAggregateWithEvents()
    {
        // Arrange
        var first = AggregateWithEvents(1, 1);
        var second = AggregateWithEvents(2, 2);
        var withoutEvents = AggregateWithEvents(3, 0);
        var publisher = new Mock<IDomainEventPublisher>(MockBehavior.Strict);
        publisher.Setup(p => p.PublishAndClear(first));
        publisher.Setup(p => p.PublishAndClear(second));

        await using var ctx = CreateContext();
        ctx.Entry(first).State = EntityState.Unchanged;
        ctx.Entry(second).State = EntityState.Unchanged;
        ctx.Entry(withoutEvents).State = EntityState.Unchanged;
        var interceptor = new DomainEventDispatcherInterceptor(publisher.Object);

        // Act
        var result = await interceptor.SavedChangesAsync(CreateEventData(ctx), 7, CancellationToken.None);

        // Assert
        result.ShouldBe(7);
        publisher.Verify(p => p.PublishAndClear(first), Times.Once);
        publisher.Verify(p => p.PublishAndClear(second), Times.Once);
        publisher.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "SavedChanges should call the publisher not at all when the context is null")]
    public void SavedChanges_WhenContextIsNull_CallsNoPublisherMethod()
    {
        // Arrange
        var publisher = new Mock<IDomainEventPublisher>(MockBehavior.Strict);
        var interceptor = new DomainEventDispatcherInterceptor(publisher.Object);

        // Act
        var result = interceptor.SavedChanges(CreateEventData(null), 42);

        // Assert
        result.ShouldBe(42);
        publisher.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "SavedChangesAsync should call the publisher not at all when the context is null")]
    public async Task SavedChangesAsync_WhenContextIsNull_CallsNoPublisherMethod()
    {
        // Arrange
        var publisher = new Mock<IDomainEventPublisher>(MockBehavior.Strict);
        var interceptor = new DomainEventDispatcherInterceptor(publisher.Object);

        // Act
        var result = await interceptor.SavedChangesAsync(CreateEventData(null), 42, CancellationToken.None);

        // Assert
        result.ShouldBe(42);
        publisher.VerifyNoOtherCalls();
    }

    // ── Failed saves ──────────────────────────────────────────────────────────

    private static TestDbContext CreateInterceptedContext(SqliteConnection connection, DomainEventDispatcherInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .EnableServiceProviderCaching(false)
            .Options;
        return new TestDbContext(options);
    }

    [Fact(DisplayName = "A save that throws should keep the aggregate's events and enqueue nothing, and a retried save should publish once (sync)")]
    public void SaveChanges_WhenSaveThrows_KeepsEventsAndRetryPublishesOnce()
    {
        // Arrange
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var publisher = new DomainEventPublisher(client.Object);
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var ctx = CreateInterceptedContext(connection, new DomainEventDispatcherInterceptor(publisher));
        var aggregate = AggregateWithEvents(1, 1);
        ctx.Aggregates.Add(aggregate);

        // Act
        // No schema yet, so the INSERT fails. SaveChangesFailed is not hooked: the interceptor never runs.
        Should.Throw<DbUpdateException>(() => ctx.SaveChanges());
        var eventsAfterFailure = aggregate.DomainEvents.Count;

        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-1");
        ctx.Database.EnsureCreated();
        ctx.SaveChanges();

        // Assert
        eventsAfterFailure.ShouldBe(1);
        client.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact(DisplayName = "A save that throws should keep the aggregate's events and enqueue nothing, and a retried save should publish once (async)")]
    public async Task SaveChangesAsync_WhenSaveThrows_KeepsEventsAndRetryPublishesOnce()
    {
        // Arrange
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        var publisher = new DomainEventPublisher(client.Object);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var ctx = CreateInterceptedContext(connection, new DomainEventDispatcherInterceptor(publisher));
        var aggregate = AggregateWithEvents(1, 1);
        ctx.Aggregates.Add(aggregate);

        // Act
        await Should.ThrowAsync<DbUpdateException>(() => ctx.SaveChangesAsync(TestContext.Current.CancellationToken));
        var eventsAfterFailure = aggregate.DomainEvents.Count;

        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-1");
        await ctx.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        eventsAfterFailure.ShouldBe(1);
        client.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    // ── No double publishing ──────────────────────────────────────────────────

    [Fact(DisplayName = "An explicit PublishAndClear after the interceptor should publish nothing more")]
    public void SavedChanges_ThenExplicitPublishAndClear_PublishesEachEventOnce()
    {
        // Arrange
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-1");
        var publisher = new DomainEventPublisher(client.Object);
        using var ctx = CreateContext();
        var aggregate = AggregateWithEvents(1, 2);
        ctx.Entry(aggregate).State = EntityState.Unchanged;

        // Act
        new DomainEventDispatcherInterceptor(publisher).SavedChanges(CreateEventData(ctx), 0);
        publisher.PublishAndClear(aggregate);

        // Assert
        client.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Exactly(2));
    }

    [Fact(DisplayName = "The interceptor after an explicit PublishAndClear should publish nothing more")]
    public void ExplicitPublishAndClear_ThenSavedChanges_PublishesEachEventOnce()
    {
        // Arrange
        var client = new Mock<IBackgroundJobClient>(MockBehavior.Strict);
        client.Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-1");
        var publisher = new DomainEventPublisher(client.Object);
        using var ctx = CreateContext();
        var aggregate = AggregateWithEvents(1, 2);
        ctx.Entry(aggregate).State = EntityState.Unchanged;

        // Act
        publisher.PublishAndClear(aggregate);
        new DomainEventDispatcherInterceptor(publisher).SavedChanges(CreateEventData(ctx), 0);

        // Assert
        client.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Exactly(2));
    }
}