using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;

namespace Neba.Website.Server;

/// <summary>
/// Extension methods for the Azure ingress in front of the app: scheme forwarding and the www-to-apex redirect.
/// </summary>
internal static class ProxyHeadersConfiguration
{
    internal static RewriteOptions WwwRedirectOptions() => new RewriteOptions().AddRedirectToNonWwwPermanent();

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Trusts <c>X-Forwarded-Proto</c> from the ingress so redirects keep the https scheme, and
        /// <c>X-Forwarded-For</c> so the app sees the visitor's IP.
        /// </summary>
        /// <remarks>
        /// The visitor's IP is passed on to the API (see <c>ClientIpForwardingHandler</c>) for rate limiting and
        /// security logs. <c>ForwardLimit</c> stays at its default of 1, so the app reads the address the ingress
        /// appended, not any value the visitor put in <c>X-Forwarded-For</c> themselves. Ingress addresses vary,
        /// so the loopback-only trust lists are cleared.
        /// </remarks>
        /// <returns>
        /// The updated service collection.
        /// </returns>
        public IServiceCollection AddProxyHeaders() =>
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Applies forwarded headers, then redirects <c>www.*</c> to the apex domain.
        /// </summary>
        /// <remarks>
        /// Must run before static files so every path redirects.
        /// </remarks>
        /// <returns>
        /// The updated application.
        /// </returns>
        public WebApplication UseProxyHeaders()
        {
            app.UseForwardedHeaders();
            app.UseRewriter(WwwRedirectOptions());

            return app;
        }
    }
}