using Audit.Core;
using Audit.Core.Providers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

using Neba.Api.Database.Interceptors;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Interceptors;

[UnitTest]
[Component("Infrastructure.Database")]
[Collection("AuditConfigurationSequential")]
public sealed class PooledAuditSaveChangesInterceptorTests : IDisposable
{
    // ── Test doubles ─────────────────────────────────────────────────────────

    private sealed class TestEntity
    {
        public int Id { get; init; } = 1;
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
    }

    private readonly InMemoryDataProvider _dataProvider = new();

    public PooledAuditSaveChangesInterceptorTests()
    {
        Audit.Core.Configuration.Setup()
            .Use(_dataProvider)
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<TestDbContext>(config => config
                .AuditEventType("EF:{context}")
                .IncludeEntityObjects(false));
    }

    public void Dispose() => Configuration.ResetCustomActions();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .EnableServiceProviderCaching(false)
            .Options;
        return new TestDbContext(options);
    }

    private static void TrackChange(TestDbContext context) =>
        context.Entry(new TestEntity()).State = EntityState.Added;

    private static DbContextEventData CreateSavingEventData(DbContext? context)
    {
        var loggingOptions = new Mock<ILoggingOptions>(MockBehavior.Loose);
        var eventDef = new Mock<EventDefinitionBase>(
            MockBehavior.Loose,
            loggingOptions.Object,
            new EventId(1),
            LogLevel.None,
            "test");
        return new DbContextEventData(eventDef.Object, (_, _) => string.Empty, context);
    }

    private static SaveChangesCompletedEventData CreateCompletedEventData(DbContext? context, int entitiesSavedCount = 0)
    {
        var loggingOptions = new Mock<ILoggingOptions>(MockBehavior.Loose);
        var eventDef = new Mock<EventDefinitionBase>(
            MockBehavior.Loose,
            loggingOptions.Object,
            new EventId(1),
            LogLevel.None,
            "test");
        return new SaveChangesCompletedEventData(eventDef.Object, (_, _) => string.Empty, context!, entitiesSavedCount);
    }

    private static DbContextErrorEventData CreateErrorEventData(DbContext? context, Exception exception)
    {
        var loggingOptions = new Mock<ILoggingOptions>(MockBehavior.Loose);
        var eventDef = new Mock<EventDefinitionBase>(
            MockBehavior.Loose,
            loggingOptions.Object,
            new EventId(1),
            LogLevel.None,
            "test");
        return new DbContextErrorEventData(eventDef.Object, (_, _) => string.Empty, context!, exception);
    }

    // ── Null context guards ────────────────────────────────────────────────────

    [Fact(DisplayName = "SavingChanges returns the result unchanged when context is null")]
    public void SavingChanges_WhenContextIsNull_ReturnsResultUnchanged()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        var result = InterceptionResult<int>.SuppressWithResult(7);

        interceptor.SavingChanges(CreateSavingEventData(null), result).ShouldBe(result);
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SavingChangesAsync returns the result unchanged when context is null")]
    public async Task SavingChangesAsync_WhenContextIsNull_ReturnsResultUnchanged()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        var result = InterceptionResult<int>.SuppressWithResult(7);

        (await interceptor.SavingChangesAsync(CreateSavingEventData(null), result, TestContext.Current.CancellationToken)).ShouldBe(result);
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SavedChanges returns the result unchanged when context is null")]
    public void SavedChanges_WhenContextIsNull_ReturnsResultUnchanged()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();

        interceptor.SavedChanges(CreateCompletedEventData(null), 3).ShouldBe(3);
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SavedChangesAsync returns the result unchanged when context is null")]
    public async Task SavedChangesAsync_WhenContextIsNull_ReturnsResultUnchanged()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();

        (await interceptor.SavedChangesAsync(CreateCompletedEventData(null), 3, TestContext.Current.CancellationToken)).ShouldBe(3);
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SaveChangesFailed does nothing when context is null")]
    public void SaveChangesFailed_WhenContextIsNull_DoesNothing()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();

        Should.NotThrow(() => interceptor.SaveChangesFailed(CreateErrorEventData(null, new InvalidOperationException())));
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SaveChangesFailedAsync does nothing when context is null")]
    public async Task SaveChangesFailedAsync_WhenContextIsNull_DoesNothing()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();

        await Should.NotThrowAsync(() => interceptor.SaveChangesFailedAsync(
            CreateErrorEventData(null, new InvalidOperationException()), TestContext.Current.CancellationToken));
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    // ── No scope registered (SavingChanges was never called for this context) ──

    [Fact(DisplayName = "SavedChanges returns the result unchanged when no scope was registered")]
    public void SavedChanges_WhenNoScopeRegistered_ReturnsResultUnchanged()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        using var context = CreateContext();

        interceptor.SavedChanges(CreateCompletedEventData(context), 5).ShouldBe(5);
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SavedChangesAsync returns the result unchanged when no scope was registered")]
    public async Task SavedChangesAsync_WhenNoScopeRegistered_ReturnsResultUnchanged()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        await using var context = CreateContext();

        (await interceptor.SavedChangesAsync(CreateCompletedEventData(context), 5, TestContext.Current.CancellationToken)).ShouldBe(5);
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SaveChangesFailed does nothing when no scope was registered")]
    public void SaveChangesFailed_WhenNoScopeRegistered_DoesNothing()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        using var context = CreateContext();

        Should.NotThrow(() => interceptor.SaveChangesFailed(CreateErrorEventData(context, new InvalidOperationException())));
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "SaveChangesFailedAsync does nothing when no scope was registered")]
    public async Task SaveChangesFailedAsync_WhenNoScopeRegistered_DoesNothing()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        await using var context = CreateContext();

        await Should.NotThrowAsync(() => interceptor.SaveChangesFailedAsync(
            CreateErrorEventData(context, new InvalidOperationException()), TestContext.Current.CancellationToken));
        _dataProvider.GetAllEvents().ShouldBeEmpty();
    }

    // ── Happy path: scope begins on Saving*, ends and is removed on Saved* ─────

    [Fact(DisplayName = "SavingChanges then SavedChanges records one audit event and removes the scope (sync)")]
    public void SavingChanges_ThenSavedChanges_RecordsAuditEventAndRemovesScope()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        using var context = CreateContext();
        TrackChange(context);

        interceptor.SavingChanges(CreateSavingEventData(context), default);
        interceptor.SavedChanges(CreateCompletedEventData(context), 1);

        _dataProvider.GetAllEvents().ShouldHaveSingleItem();

        // Scope was removed after the first SavedChanges call, so a second call is a no-op.
        interceptor.SavedChanges(CreateCompletedEventData(context), 1);
        _dataProvider.GetAllEvents().Count.ShouldBe(1);
    }

    [Fact(DisplayName = "SavingChangesAsync then SavedChangesAsync records one audit event and removes the scope (async)")]
    public async Task SavingChangesAsync_ThenSavedChangesAsync_RecordsAuditEventAndRemovesScope()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        await using var context = CreateContext();
        TrackChange(context);

        await interceptor.SavingChangesAsync(CreateSavingEventData(context), default, TestContext.Current.CancellationToken);
        await interceptor.SavedChangesAsync(CreateCompletedEventData(context), 1, TestContext.Current.CancellationToken);

        _dataProvider.GetAllEvents().ShouldHaveSingleItem();

        // Scope was removed after the first SavedChangesAsync call, so a second call is a no-op.
        await interceptor.SavedChangesAsync(CreateCompletedEventData(context), 1, TestContext.Current.CancellationToken);
        _dataProvider.GetAllEvents().Count.ShouldBe(1);
    }

    // ── Failure path: scope begins on Saving*, ends and is removed on Failed* ──

    [Fact(DisplayName = "SavingChanges then SaveChangesFailed records the audit event with the exception and removes the scope (sync)")]
    public void SavingChanges_ThenSaveChangesFailed_RecordsAuditEventAndRemovesScope()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        using var context = CreateContext();
        TrackChange(context);
        var exception = new InvalidOperationException("save failed");

        interceptor.SavingChanges(CreateSavingEventData(context), default);
        interceptor.SaveChangesFailed(CreateErrorEventData(context, exception));

        _dataProvider.GetAllEvents().ShouldHaveSingleItem();

        // Scope was removed after the first SaveChangesFailed call, so a second call is a no-op.
        interceptor.SaveChangesFailed(CreateErrorEventData(context, exception));
        _dataProvider.GetAllEvents().Count.ShouldBe(1);
    }

    [Fact(DisplayName = "SavingChangesAsync then SaveChangesFailedAsync records the audit event with the exception and removes the scope (async)")]
    public async Task SavingChangesAsync_ThenSaveChangesFailedAsync_RecordsAuditEventAndRemovesScope()
    {
        var interceptor = new PooledAuditSaveChangesInterceptor();
        await using var context = CreateContext();
        TrackChange(context);
        var exception = new InvalidOperationException("save failed");

        await interceptor.SavingChangesAsync(CreateSavingEventData(context), default, TestContext.Current.CancellationToken);
        await interceptor.SaveChangesFailedAsync(CreateErrorEventData(context, exception), TestContext.Current.CancellationToken);

        _dataProvider.GetAllEvents().ShouldHaveSingleItem();

        // Scope was removed after the first SaveChangesFailedAsync call, so a second call is a no-op.
        await interceptor.SaveChangesFailedAsync(CreateErrorEventData(context, exception), TestContext.Current.CancellationToken);
        _dataProvider.GetAllEvents().Count.ShouldBe(1);
    }
}