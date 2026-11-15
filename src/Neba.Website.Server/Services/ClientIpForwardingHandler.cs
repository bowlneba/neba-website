namespace Neba.Website.Server.Services;

/// <summary>
/// Passes the visitor's IP to the API in <c>X-Forwarded-For</c>, so the API's rate limiting and logs see the
/// visitor and not this server. The API only trusts the header from private networks.
/// </summary>
/// <remarks>
/// Inside a circuit, <see cref="IHttpContextAccessor.HttpContext"/> is the request that opened the connection,
/// which carries the visitor's address for the whole circuit.
/// </remarks>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
internal sealed class ClientIpForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string ForwardedForHeader = "X-Forwarded-For";

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

        if (ip is not null)
        {
            request.Headers.Remove(ForwardedForHeader);
            request.Headers.TryAddWithoutValidation(ForwardedForHeader, ip.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}