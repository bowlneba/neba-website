using System.Net;

using Microsoft.AspNetCore.Http;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Services;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services.ClientIpForwardingHandler")]
public sealed class ClientIpForwardingHandlerTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static Mock<IHttpContextAccessor> AccessorFor(IPAddress? ip)
    {
        var accessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

        if (ip is null)
        {
            accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        }
        else
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = ip;
            accessor.SetupGet(a => a.HttpContext).Returns(context);
        }

        return accessor;
    }

    private static async Task<HttpRequestMessage> SendThroughHandlerAsync(IPAddress? ip, Action<HttpRequestMessage>? configure = null)
    {
        var inner = new CapturingHandler();
        using var handler = new ClientIpForwardingHandler(AccessorFor(ip).Object) { InnerHandler = inner };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/security/password/forgot");
        configure?.Invoke(request);

        using var response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        return inner.Request!;
    }

    [Fact(DisplayName = "SendAsync should set X-Forwarded-For to the visitor's IP")]
    public async Task SendAsync_ShouldSetForwardedFor_FromHttpContext()
    {
        // Arrange & Act
        var sent = await SendThroughHandlerAsync(IPAddress.Parse("203.0.113.7"));

        // Assert
        sent.Headers.GetValues("X-Forwarded-For").ShouldBe(["203.0.113.7"]);
    }

    [Fact(DisplayName = "SendAsync should replace an X-Forwarded-For header already on the request")]
    public async Task SendAsync_ShouldReplaceExistingForwardedFor()
    {
        // Arrange & Act
        var sent = await SendThroughHandlerAsync(
            IPAddress.Parse("203.0.113.7"),
            request => request.Headers.TryAddWithoutValidation("X-Forwarded-For", "192.0.2.99"));

        // Assert
        sent.Headers.GetValues("X-Forwarded-For").ShouldBe(["203.0.113.7"]);
    }

    [Fact(DisplayName = "SendAsync should add no header when there is no HTTP context")]
    public async Task SendAsync_ShouldAddNoHeader_WhenNoHttpContext()
    {
        // Arrange & Act
        var sent = await SendThroughHandlerAsync(null);

        // Assert
        sent.Headers.Contains("X-Forwarded-For").ShouldBeFalse();
    }

    [Fact(DisplayName = "SendAsync should add no header when the connection has no remote address")]
    public async Task SendAsync_ShouldAddNoHeader_WhenNoRemoteAddress()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        accessor.SetupGet(a => a.HttpContext).Returns(new DefaultHttpContext());
        var inner = new CapturingHandler();
        using var handler = new ClientIpForwardingHandler(accessor.Object) { InnerHandler = inner };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/x");

        // Act
        using var response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        inner.Request!.Headers.Contains("X-Forwarded-For").ShouldBeFalse();
    }
}