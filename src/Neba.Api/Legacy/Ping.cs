using Hangfire;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;

using Neba.Api.Identity;

namespace Neba.Api.Legacy;

internal static class PingEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapPing()
        {
            app.MapPost("/ping", ([FromServices] IBackgroundJobClient jobs) =>
            {
                jobs.Enqueue<PongJob>(job => job.PongAsync(CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed class PongJob(
    IHttpClientFactory httpClientFactory,
    IServer server,
    ILogger<PongJob> logger)
{
    public async Task PongAsync(CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var baseUrl = ResolveSelfBaseUrl(server);
        if (baseUrl is null)
        {
            logger.LogPongCouldNotResolveSelfUrl();
            return;
        }

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            using var response = await client.GetAsync(new Uri(baseUrl, "/health"), ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            logger.LogPong((int)response.StatusCode, body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogPongFailed(ex.Message);
        }
    }

    // Stryker disable once Block : trivial address normalization, not worth a dedicated unit test file
    private static Uri? ResolveSelfBaseUrl(IServer server)
    {
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
        if (address is null)
        {
            return null;
        }

        // Kestrel reports its bound addresses using whatever wildcard host it was configured
        // with (e.g. "http://+:8080" from ASPNETCORE_URLS becomes "http://[::]:8080" in
        // IServerAddressesFeature) - that's a bind address, not something HttpClient can
        // connect to. Swap any wildcard host for "localhost" before using it as a call target.
        var uri = new Uri(address
            .Replace("+", "localhost", StringComparison.Ordinal)
            .Replace("*", "localhost", StringComparison.Ordinal)
            .Replace("[::]", "localhost", StringComparison.Ordinal)
            .Replace("0.0.0.0", "localhost", StringComparison.Ordinal));
        return uri;
    }
}

internal static partial class PongJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Pong: GET /health returned {StatusCode}: {Body}")]
    public static partial void LogPong(this ILogger<PongJob> logger, int statusCode, string body);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Pong: GET /health failed: {Reason}")]
    public static partial void LogPongFailed(this ILogger<PongJob> logger, string reason);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Pong: could not resolve the API's own base URL from IServerAddressesFeature; skipping self-ping.")]
    public static partial void LogPongCouldNotResolveSelfUrl(this ILogger<PongJob> logger);
}
