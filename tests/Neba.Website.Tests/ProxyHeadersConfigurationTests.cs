using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Neba.TestFactory.Attributes;
using Neba.Website.Server;

namespace Neba.Website.Tests;

[UnitTest]
[Component("Website.ProxyHeadersConfiguration")]
public sealed class ProxyHeadersConfigurationTests
{
    private static ForwardedHeadersOptions ResolveForwardedHeadersOptions()
    {
        var services = new ServiceCollection();
        services.AddProxyHeaders();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    private static DefaultHttpContext CreateContext(string host, string scheme = "https", string path = "/", string query = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        return context;
    }

    [Fact(DisplayName = "AddProxyHeaders should forward only X-Forwarded-Proto")]
    public void AddProxyHeaders_ShouldForwardOnlyProto()
    {
        // Act
        var options = ResolveForwardedHeadersOptions();

        // Assert
        options.ForwardedHeaders.ShouldBe(ForwardedHeaders.XForwardedProto);
    }

    [Fact(DisplayName = "AddProxyHeaders should clear the loopback-only trust lists")]
    public void AddProxyHeaders_ShouldClearTrustLists()
    {
        // Act
        var options = ResolveForwardedHeadersOptions();

        // Assert
        options.KnownIPNetworks.ShouldBeEmpty();
        options.KnownProxies.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Forwarded headers should apply X-Forwarded-Proto from a non-loopback caller")]
    public async Task ForwardedHeaders_ShouldApplyProto_WhenCallerIsNotLoopback()
    {
        // Arrange
        var context = CreateContext("bowlneba.com", scheme: "http");
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(ResolveForwardedHeadersOptions()));

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Request.Scheme.ShouldBe("https");
    }

    [Fact(DisplayName = "Forwarded headers should ignore X-Forwarded-For so a caller cannot spoof its IP")]
    public async Task ForwardedHeaders_ShouldIgnoreForwardedFor()
    {
        // Arrange
        var context = CreateContext("bowlneba.com");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.7";
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(ResolveForwardedHeadersOptions()));

        // Act
        await middleware.Invoke(context);

        // Assert
        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse("203.0.113.10"));
    }

    [Theory(DisplayName = "WwwRedirectOptions should permanently redirect www hosts to the apex, keeping path and query")]
    [InlineData("www.bowlneba.com", "https", "/history/champions", "?year=2024", "https://bowlneba.com/history/champions?year=2024", TestDisplayName = "https with path and query")]
    [InlineData("www.bowlneba.com", "https", "/", "", "https://bowlneba.com/", TestDisplayName = "https root")]
    [InlineData("www.bowlneba.com", "http", "/stats", "", "http://bowlneba.com/stats", TestDisplayName = "keeps the request scheme")]
    public async Task WwwRedirectOptions_ShouldRedirectToApex_WhenHostStartsWithWww(
        string host, string scheme, string path, string query, string expectedLocation)
    {
        // Arrange
        var context = CreateContext(host, scheme, path, query);
        var nextCalled = false;

        // Act
        await InvokeRewriteAsync(context, () => nextCalled = true);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status308PermanentRedirect);
        context.Response.Headers.Location.ToString().ShouldBe(expectedLocation);
        nextCalled.ShouldBeFalse();
    }

    [Fact(DisplayName = "WwwRedirectOptions should pass through requests already on the apex")]
    public async Task WwwRedirectOptions_ShouldPassThrough_WhenHostIsApex()
    {
        // Arrange
        var context = CreateContext("bowlneba.com", path: "/stats");
        var nextCalled = false;

        // Act
        await InvokeRewriteAsync(context, () => nextCalled = true);

        // Assert
        nextCalled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact(DisplayName = "UseProxyHeaders should return the same application")]
    public async Task UseProxyHeaders_ShouldReturnSameApplication()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddProxyHeaders();
        await using var app = builder.Build();

        // Act
        var result = app.UseProxyHeaders();

        // Assert
        result.ShouldBeSameAs(app);
    }

    private static Task InvokeRewriteAsync(HttpContext context, Action onNext)
    {
        var environment = new Mock<IWebHostEnvironment>(MockBehavior.Strict);
        environment.Setup(e => e.WebRootFileProvider).Returns(new NullFileProvider());

        var middleware = new RewriteMiddleware(
            _ =>
            {
                onNext();
                return Task.CompletedTask;
            },
            environment.Object,
            NullLoggerFactory.Instance,
            Options.Create(ProxyHeadersConfiguration.WwwRedirectOptions()));

        return middleware.Invoke(context);
    }
}