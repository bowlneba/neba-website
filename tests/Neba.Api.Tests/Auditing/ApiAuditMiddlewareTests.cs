using Audit.Core;
using Audit.Core.Providers;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Auditing;
using Neba.Api.Identity;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
[Collection("AuditConfigurationSequential")]
public sealed class ApiAuditMiddlewareTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new(MockBehavior.Strict);
    private readonly FakeTimeProvider _fakeTimeProvider = new(new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeLogger<ApiAuditMiddleware> _logger = new();

    public ApiAuditMiddlewareTests()
        => Configuration.Setup()
            .Use(new InMemoryDataProvider())
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

    private static InMemoryDataProvider Provider => (InMemoryDataProvider)Configuration.DataProvider;

    // ── ShouldSkip ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "InvokeAsync calls next and does not create an audit scope when the request is a GET")]
    public async Task InvokeAsync_ShouldCallNextAndSkipAudit_WhenRequestIsGet()
    {
        // Arrange
        var nextCalled = false;
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/bowlers";

        var sut = CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
        Provider.GetAllEvents().ShouldBeEmpty();
    }

    [Theory(DisplayName = "InvokeAsync calls next and does not create an audit scope when the path is excluded")]
    [InlineData("/health")]
    [InlineData("/scalar")]
    [InlineData("/background-jobs")]
    [InlineData("/debug")]
    public async Task InvokeAsync_ShouldCallNextAndSkipAudit_WhenPathIsExcluded(string excludedPath)
    {
        // Arrange
        var nextCalled = false;
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = excludedPath + "/details";

        var sut = CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
        Provider.GetAllEvents().ShouldBeEmpty();
    }

    [Fact(DisplayName = "InvokeAsync skips audit for excluded paths regardless of casing")]
    public async Task InvokeAsync_ShouldSkipAudit_WhenExcludedPathHasDifferentCasing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/HEALTH/status";

        var sut = CreateSut();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        Provider.GetAllEvents().ShouldBeEmpty();
    }

    // ── Audit scope creation ─────────────────────────────────────────────────

    [Fact(DisplayName = "InvokeAsync creates an audit event with request and actor details when the request is not skipped")]
    public async Task InvokeAsync_ShouldCreateAuditEventWithExpectedFields_WhenRequestIsNotSkipped()
    {
        // Arrange
        _currentUserServiceMock.SetupGet(s => s.ActorId).Returns("actor-1");

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/bowlers";
        context.TraceIdentifier = "trace-1";

        var sut = CreateSut(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(context);

        // Assert
        var auditEvent = Provider.GetAllEvents().ShouldHaveSingleItem();
        auditEvent.CustomFields["Route"].ShouldBe("/bowlers");
        auditEvent.CustomFields["Method"].ShouldBe("POST");
        auditEvent.CustomFields["ActorId"].ShouldBe("actor-1");
        auditEvent.CustomFields["CorrelationId"].ShouldBe("trace-1");
        auditEvent.CustomFields["StartedAt"].ShouldBe(_fakeTimeProvider.GetUtcNow());
        auditEvent.CustomFields["StatusCode"].ShouldBe(StatusCodes.Status201Created);
    }

    [Fact(DisplayName = "InvokeAsync records the elapsed time between request start and completion")]
    public async Task InvokeAsync_ShouldRecordElapsedMs_BetweenStartAndCompletion()
    {
        // Arrange
        _currentUserServiceMock.SetupGet(s => s.ActorId).Returns("actor-1");

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/bowlers";

        var sut = CreateSut(_ =>
        {
            _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(250));
            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(context);

        // Assert
        var auditEvent = Provider.GetAllEvents().ShouldHaveSingleItem();
        auditEvent.CustomFields["ElapsedMs"].ShouldBe(250d);
    }

    [Fact(DisplayName = "InvokeAsync calls next even when the request is audited")]
    public async Task InvokeAsync_ShouldCallNext_WhenRequestIsAudited()
    {
        // Arrange
        _currentUserServiceMock.SetupGet(s => s.ActorId).Returns("actor-1");

        var nextCalled = false;
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/bowlers";

        var sut = CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
    }

    // ── Resilience ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "InvokeAsync logs a warning and still calls next when audit scope creation fails")]
    public async Task InvokeAsync_ShouldLogWarningAndCallNext_WhenAuditScopeCreationFails()
    {
        // Arrange
        Configuration.Setup()
            .Use(new ThrowingOnInsertDataProvider())
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

        _currentUserServiceMock.SetupGet(s => s.ActorId).Returns("actor-1");

        var nextCalled = false;
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/bowlers";

        var sut = CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
        _logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    [Fact(DisplayName = "InvokeAsync logs a warning without throwing when audit scope completion fails")]
    public async Task InvokeAsync_ShouldLogWarningAndNotThrow_WhenAuditScopeCompletionFails()
    {
        // Arrange
        Configuration.Setup()
            .Use(new ThrowingOnReplaceDataProvider())
            .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

        _currentUserServiceMock.SetupGet(s => s.ActorId).Returns("actor-1");

        var nextCalled = false;
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/bowlers";

        var sut = CreateSut(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
        _logger.Collector.GetSnapshot().ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Warning);
    }

    private ApiAuditMiddleware CreateSut(RequestDelegate? next = null)
        => new(next ?? (_ => Task.CompletedTask), _currentUserServiceMock.Object, _fakeTimeProvider, _logger);

    private sealed class ThrowingOnInsertDataProvider : AuditDataProvider
    {
        public override object InsertEvent(AuditEvent auditEvent)
            => throw new InvalidOperationException("insert failed");

        public override Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("insert failed");
    }

    private sealed class ThrowingOnReplaceDataProvider : AuditDataProvider
    {
        public override object InsertEvent(AuditEvent auditEvent) => "event-id";

        public override Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => Task.FromResult<object>("event-id");

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
            => throw new InvalidOperationException("replace failed");

        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("replace failed");
    }
}