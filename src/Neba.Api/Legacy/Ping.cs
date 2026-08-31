using System.Diagnostics.CodeAnalysis;

using Hangfire;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;

using Neba.Api.BackgroundJobs;
using Neba.Api.Compliance;
using Neba.Api.Discord;
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
    IDiscordNotifier discordNotifier,
    ILogger<PongJob> logger)
{
    // The job's own Succeeded/Failed state is the health signal - Hangfire's Console output can't be
    // relied on here (its Postgres storage writes go through a System.Transactions.TransactionScope
    // commit, which needs prepared-transaction support this Postgres instance doesn't have - see
    // CLAUDE.md's "Hangfire PostgreSql EnableTransactionScopeEnlistment" entry for the related
    // incident). A non-2xx /health response, or a transport failure reaching it, throws so the job
    // fails; anything else it means the API answered its own health check successfully.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Any failure reaching /health means the legacy bridge is down; every cause gets the same Discord alert before the job still fails visibly.")]
    [SkipDiscordJobFailureAlert] // Posts its own alert below on every failed attempt; see the attribute's doc comment.
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

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Pong: GET /health returned {(int)response.StatusCode}: {body}");
            }

            // Posts on every successful ping, not just the first - this endpoint exists to give a
            // visible, on-demand signal that the Discord bridge is connected, so a success here
            // should always be confirmed the same way a failure would be alerted.
            await discordNotifier.NotifyAsync(
                new DiscordAlert(DiscordAlertSeverity.Info, "Legacy bridge ping succeeded", "Pong: GET /health returned 200."),
                ct);
        }
        // Excludes OperationCanceledException triggered by the caller's own ct (normal host
        // shutdown / Hangfire job abortion) - those aren't a /health failure and shouldn't fire a
        // false Critical Discord alert, matching DiscordNotifier.NotifyAsync's identical guard. A
        // client-side HttpClient.Timeout expiry also throws an OperationCanceledException, but via
        // its own internal token, not ct - ct.IsCancellationRequested is false in that case, so a
        // real timeout is still caught, logged, and alerted below.
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogPongFailed(ex.Message);

            var alert = new DiscordAlert(
                DiscordAlertSeverity.Critical,
                "Legacy bridge ping failed",
                DiscordMessageRedactor.Redact(ex.Message)
            );

            // CancellationToken.None, not ct - ct is what just failed (a timeout is one of the
            // two failure modes this alert reports), so posting under it would race the alert
            // against the same cancellation that triggered it.
            await discordNotifier.NotifyAsync(alert, CancellationToken.None);

            throw;
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

        const string localhost = "localhost";

        // Kestrel reports its bound addresses using whatever wildcard host it was configured
        // with (e.g. "http://+:8080" from ASPNETCORE_URLS becomes "http://[::]:8080" in
        // IServerAddressesFeature) - that's a bind address, not something HttpClient can
        // connect to. Swap any wildcard host for "localhost" before using it as a call target.
        var uri = new Uri(address
            .Replace("+", localhost, StringComparison.Ordinal)
            .Replace("*", localhost, StringComparison.Ordinal)
            .Replace("[::]", localhost, StringComparison.Ordinal)
            .Replace("0.0.0.0", localhost, StringComparison.Ordinal));
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