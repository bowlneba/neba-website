using System.Data.Common;
using System.Diagnostics;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Neba.Infrastructure.Database.Interceptors;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Database.Interceptors;

[UnitTest]
[Component("Infrastructure.Database")]
public sealed class QueryTagEnrichmentInterceptorTests
{
    private static QueryTagEnrichmentInterceptor CreateInterceptor(HttpContext? ctx)
    {
        var accessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        accessor.SetupGet(a => a.HttpContext).Returns(ctx);
        return new QueryTagEnrichmentInterceptor(accessor.Object);
    }

    private static Mock<DbCommand> CreateCommand(string sql = "SELECT 1")
    {
        // MockBehavior.Loose: DbCommand inherits Component whose finalizer calls Dispose(false).
        // Strict would require a setup for that call, but Protected().Setup can't disambiguate
        // Dispose(bool) from the public Dispose(). Loose is correct here — the test only asserts
        // on CommandText, not on which members the interceptor accesses.
        var cmd = new Mock<DbCommand>(MockBehavior.Loose);
        cmd.SetupProperty(c => c.CommandText, sql);
        return cmd;
    }

    private static DefaultHttpContext AuthenticatedContext(string userId = "user-123")
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)]));
        return ctx;
    }

    // ── No-op when HttpContext is null ────────────────────────────────────────

    [Fact(DisplayName = "Does not modify command when HttpContext is null (sync)")]
    public void ReaderExecuting_WhenHttpContextIsNull_DoesNotModifyCommand()
    {
        const string sql = "SELECT 1";
        var cmd = CreateCommand(sql);

        CreateInterceptor(null).ReaderExecuting(cmd.Object, null!, default);

        cmd.Object.CommandText.ShouldBe(sql);
    }

    [Fact(DisplayName = "Does not modify command when HttpContext is null (async)")]
    public async Task ReaderExecutingAsync_WhenHttpContextIsNull_DoesNotModifyCommand()
    {
        const string sql = "SELECT 1";
        var cmd = CreateCommand(sql);

        await CreateInterceptor(null).ReaderExecutingAsync(cmd.Object, null!, default, CancellationToken.None);

        cmd.Object.CommandText.ShouldBe(sql);
    }

    // ── Full comment format ───────────────────────────────────────────────────

    [Fact(DisplayName = "ReaderExecuting prepends comment with user, endpoint, and Activity trace ID")]
    public void ReaderExecuting_WithFullContext_PrependsComment()
    {
        var cmd = CreateCommand();
        var ctx = AuthenticatedContext("alice");
        ctx.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "GET /bowlers"));

        var activitySource = new Activity("test");
        activitySource.Start();
        using var activity = activitySource;
        var expectedTraceId = Activity.Current!.TraceId.ToString();

        CreateInterceptor(ctx).ReaderExecuting(cmd.Object, null!, default);

        cmd.Object.CommandText.ShouldBe($"/* user:alice endpoint:GET /bowlers trace:{expectedTraceId} */\nSELECT 1");
    }

    [Fact(DisplayName = "ReaderExecutingAsync prepends comment with user, endpoint, and Activity trace ID")]
    public async Task ReaderExecutingAsync_WithFullContext_PrependsComment()
    {
        var cmd = CreateCommand();
        var ctx = AuthenticatedContext("alice");
        ctx.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "GET /bowlers"));

        var activitySource = new Activity("test");
        activitySource.Start();
        using var activity = activitySource;
        var expectedTraceId = Activity.Current!.TraceId.ToString();

        await CreateInterceptor(ctx).ReaderExecutingAsync(cmd.Object, null!, default, CancellationToken.None);

        cmd.Object.CommandText.ShouldBe($"/* user:alice endpoint:GET /bowlers trace:{expectedTraceId} */\nSELECT 1");
    }

    // ── Fallbacks ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Falls back to 'anon' when user has no NameIdentifier claim")]
    public void ReaderExecuting_WhenNoNameIdentifierClaim_UsesAnonFallback()
    {
        var cmd = CreateCommand();
        var ctx = new DefaultHttpContext();
        ctx.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "GET /test"));
        ctx.TraceIdentifier = "req-abc";
        Activity.Current = null;

        CreateInterceptor(ctx).ReaderExecuting(cmd.Object, null!, default);

        cmd.Object.CommandText.ShouldStartWith("/* user:anon ");
    }

    [Fact(DisplayName = "Falls back to 'unknown' when no endpoint is set")]
    public void ReaderExecuting_WhenNoEndpoint_UsesUnknownFallback()
    {
        var cmd = CreateCommand();
        var ctx = AuthenticatedContext("alice");
        ctx.TraceIdentifier = "req-abc";
        Activity.Current = null;

        CreateInterceptor(ctx).ReaderExecuting(cmd.Object, null!, default);

        cmd.Object.CommandText.ShouldContain("endpoint:unknown");
    }

    [Fact(DisplayName = "Falls back to ctx.TraceIdentifier when Activity.Current is null")]
    public void ReaderExecuting_WhenNoActivityCurrent_UsesTraceIdentifierFallback()
    {
        var cmd = CreateCommand();
        var ctx = AuthenticatedContext("alice");
        ctx.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "GET /test"));
        ctx.TraceIdentifier = "req-abc-123";
        Activity.Current = null;

        CreateInterceptor(ctx).ReaderExecuting(cmd.Object, null!, default);

        cmd.Object.CommandText.ShouldContain("trace:req-abc-123");
    }

    // ── Comment breakout sanitization ─────────────────────────────────────────

    [Fact(DisplayName = "Strips '*/' from userId to prevent comment breakout")]
    public void ReaderExecuting_WhenUserIdContainsCommentEnd_StripsIt()
    {
        var cmd = CreateCommand();
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "anon */ DROP TABLE users; --")]));
        ctx.TraceIdentifier = "req-abc";
        Activity.Current = null;

        CreateInterceptor(ctx).ReaderExecuting(cmd.Object, null!, default);

        // If stripping works, the comment closes once at the end; nothing dangerous leaks after it.
        var afterComment = cmd.Object.CommandText[(cmd.Object.CommandText.IndexOf("*/", StringComparison.Ordinal) + 2)..];
        afterComment.ShouldNotContain("DROP TABLE", customMessage: "comment breakout must be stripped from userId");
    }

    [Fact(DisplayName = "Strips '*/' from endpoint to prevent comment breakout")]
    public void ReaderExecuting_WhenEndpointContainsCommentEnd_StripsIt()
    {
        var cmd = CreateCommand();
        var ctx = AuthenticatedContext("alice");
        ctx.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "GET /test */ DROP TABLE users; --"));
        ctx.TraceIdentifier = "req-abc";
        Activity.Current = null;

        CreateInterceptor(ctx).ReaderExecuting(cmd.Object, null!, default);

        var afterComment = cmd.Object.CommandText[(cmd.Object.CommandText.IndexOf("*/", StringComparison.Ordinal) + 2)..];
        afterComment.ShouldNotContain("DROP TABLE", customMessage: "comment breakout must be stripped from endpoint");
    }

    [Fact(DisplayName = "Strips '*/' from traceId to prevent comment breakout")]
    public void ReaderExecuting_WhenTraceIdContainsCommentEnd_StripsIt()
    {
        var cmd = CreateCommand();
        var ctx = AuthenticatedContext("alice");
        ctx.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(), "GET /test"));
        ctx.TraceIdentifier = "req-abc */ DROP TABLE users; --";
        Activity.Current = null;

        CreateInterceptor(ctx).ReaderExecuting(cmd.Object, null!, default);

        var afterComment = cmd.Object.CommandText[(cmd.Object.CommandText.IndexOf("*/", StringComparison.Ordinal) + 2)..];
        afterComment.ShouldNotContain("DROP TABLE", customMessage: "comment breakout must be stripped from traceId");
    }
}