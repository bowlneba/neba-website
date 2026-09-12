using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

namespace Neba.Api.RateLimiting;

internal static class RateLimitingConfiguration
{
    internal const string PublicPolicy = "public";

    extension(IServiceCollection services)
    {
        internal void AddRateLimiting(IConfiguration config)
        {
            var permitLimit = config.GetValue("RateLimiting:PermitLimit", 100);
            var windowSeconds = config.GetValue("RateLimiting:WindowSeconds", 60);

            // Authenticated callers aren't meaningfully limited by this policy - they get a high
            // ceiling that only exists to blunt a compromised/malicious authenticated client, not
            // to constrain normal usage. Partitioned per-user (see below) rather than per-IP,
            // since every request from Neba.Website.Server arrives here as one server-to-server
            // connection - partitioning authenticated traffic by IP would still lump every signed-in
            // user behind that one connection into a single shared bucket.
            var authenticatedPermitLimit = config.GetValue("RateLimiting:AuthenticatedPermitLimit", 1000);
            var authenticatedWindowSeconds = config.GetValue("RateLimiting:AuthenticatedWindowSeconds", 60);

            if (permitLimit <= 0)
            {
                throw new InvalidOperationException(
                    $"RateLimiting:PermitLimit must be greater than zero (configured value: {permitLimit}).");
            }

            if (windowSeconds <= 0)
            {
                throw new InvalidOperationException(
                    $"RateLimiting:WindowSeconds must be greater than zero (configured value: {windowSeconds}).");
            }

            if (authenticatedPermitLimit <= 0)
            {
                throw new InvalidOperationException(
                    $"RateLimiting:AuthenticatedPermitLimit must be greater than zero (configured value: {authenticatedPermitLimit}).");
            }

            if (authenticatedWindowSeconds <= 0)
            {
                throw new InvalidOperationException(
                    $"RateLimiting:AuthenticatedWindowSeconds must be greater than zero (configured value: {authenticatedWindowSeconds}).");
            }

            // Trust X-Forwarded-For only from RFC 1918 private networks (Azure load balancers
            // and Container Apps infrastructure). RemoteIpAddress is rewritten by
            // UseForwardedHeaders() before it reaches the rate limiter.
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
#pragma warning disable S1313 // RFC 1918 private ranges — Azure load balancers and Container Apps infrastructure
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
#pragma warning restore S1313
            });

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                    }

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too Many Requests",
                            Detail = "Rate limit exceeded. Please retry after the specified time.",
                        },
                        options: null,
                        contentType: MediaTypeNames.Application.ProblemJson,
                        cancellationToken);
                };

                options.AddPolicy(PublicPolicy, context =>
                {
                    if (context.User.Identity?.IsAuthenticated == true)
                    {
                        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

                        return RateLimitPartition.GetFixedWindowLimiter($"user:{userId}",
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = authenticatedPermitLimit,
                                Window = TimeSpan.FromSeconds(authenticatedWindowSeconds),
                                QueueLimit = 0,
                            });
                    }

                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter($"ip:{ip}",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(windowSeconds),
                            QueueLimit = 0,
                        });
                });
            });
        }
    }
}