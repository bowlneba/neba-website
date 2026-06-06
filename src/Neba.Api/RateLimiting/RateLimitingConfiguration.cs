using System.Globalization;
using System.Net;
using System.Net.Mime;
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

            // Trust X-Forwarded-For only from RFC 1918 private networks (Azure load balancers
            // and Container Apps infrastructure). RemoteIpAddress is rewritten by
            // UseForwardedHeaders() before it reaches the rate limiter.
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
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
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(ip,
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