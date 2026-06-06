using System.Globalization;
using System.Threading.RateLimiting;

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

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = (context, _) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                    }

                    return ValueTask.CompletedTask;
                };

                options.AddPolicy(PublicPolicy, context =>
                {
                    var ip = context.Request.Headers["X-Forwarded-For"]
                        .FirstOrDefault()
                        ?.Split(',')[0]
                        .Trim()
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

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
