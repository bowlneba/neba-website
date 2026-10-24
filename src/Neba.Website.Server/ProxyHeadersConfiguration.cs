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
        /// Trusts <c>X-Forwarded-Proto</c> from the ingress so redirects keep the https scheme.
        /// </summary>
        /// <remarks>
        /// Only the scheme is forwarded. The app never reads the client IP, so <c>X-Forwarded-For</c> stays
        /// ignored and a caller cannot spoof it. Ingress addresses vary, so the loopback-only trust lists are cleared.
        /// </remarks>
        /// <returns>
        /// The updated service collection.
        /// </returns>
        public IServiceCollection AddProxyHeaders() =>
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
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