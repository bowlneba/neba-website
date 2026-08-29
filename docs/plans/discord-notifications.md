# Discord Notifications

Adds a "must-look-now" Discord channel (`#api-logs`) that the API posts to for a small set of
high-severity, otherwise-invisible failures. Not a general log sink — routine activity (domain
audit events, successful jobs, cache fallbacks) stays out on purpose. Two phases: first the
wiring (how the API talks to Discord), then the candidate call sites (what actually gets posted
and why).

## Status

**Phase 1 (wiring) implemented.** `src/Neba.Api/Discord/` has `IDiscordNotifier`/`DiscordNotifier`,
`DiscordSettings`, and `DiscordConfiguration.AddDiscord()`, called from
`InfrastructureConfiguration.AddInfrastructure()`. Tests in `tests/Neba.Api.Tests/Discord/`.
`Discord:WebhookUrl` is seeded into Key Vault (`Discord--WebhookUrl`) from the `DISCORD_WEBHOOK_URL`
GitHub secret in `cd.yml`, same as the other secrets.

Two deviations from the sketch below, kept because they turned out better in practice:

- `DiscordAlertSeverity` is an `Ardalis.SmartEnum` (`Info`/`Warning`/`Critical`), not a plain
  `enum` — matches the rest of the codebase's SmartEnum convention (see `PhoneNumberType`,
  `SponsorTier`) and lets the color live on the enum member itself.
- Colors are a `DiscordColor` readonly record struct (`R`/`G`/`B` + `RawValue`) instead of raw hex
  `const int`s, and the embed payload is built from `DiscordEmbed`/`DiscordEmbedField` records
  instead of anonymous types — both ideas borrowed from `Discord.Net`'s `Color`/`EmbedBuilder`
  shape without taking the dependency (see Open Questions in the original design discussion).

Phase 2 (candidate events) not started.

## Goals / Non-Goals

- **Goal**: a human sees, within minutes, that something needs attention — a stuck legacy sync,
  a season/tournament pipeline that silently stopped, the notification system's own failure.
- **Goal**: posting to Discord must never be able to break or slow down the operation that
  triggered it. Same philosophy as `ResilientAuditDataProvider` — best-effort, swallow and log.
- **Non-goal**: replacing Application Insights / structured logging. Discord is for the handful
  of events where nobody would otherwise look until a user complains.
- **Non-goal**: a general-purpose logger sink or `ILoggerProvider`. Call sites opt in explicitly
  (see Phase 2) rather than a level/category filter deciding what's "important" implicitly.

---

## Phase 1 — Wiring

### Shape: mirror `IEmailSender`, harden like `ResilientAuditDataProvider`

`src/Neba.Api/Email/IEmailSender.cs` is the closest existing analog — a single-method internal
interface, `AddTransient` DI registration, injected directly into command/job handlers. Discord
follows the same shape, with one difference: `IEmailSender.SendAsync` is allowed to throw and
propagate today; the Discord notifier must not. A failed Discord post is not allowed to fail the
legacy sync job / exception handler / audit path that's trying to report through it — that would
turn an alerting mechanism into a new failure mode. So the never-throw behavior lives inside the
notifier itself, not left to callers.

```csharp
// src/Neba.Api/Discord/IDiscordNotifier.cs
internal interface IDiscordNotifier
{
    Task NotifyAsync(DiscordAlert alert, CancellationToken cancellationToken);
}
```

`DiscordAlert` is a small record: severity, title, a short body, and an optional dictionary of
fields (matches Discord's embed field shape — see Message Format below).

```csharp
// src/Neba.Api/Discord/DiscordAlert.cs
internal enum DiscordAlertSeverity
{
    Warning,
    Critical
}

internal sealed record DiscordAlert(
    DiscordAlertSeverity Severity,
    string Title,
    string Description,
    IReadOnlyDictionary<string, string>? Fields = null);
```

Plain values, not a builder — every Phase 2 call site constructs one inline
(`new DiscordAlert(DiscordAlertSeverity.Critical, "Legacy bridge ping failed", exception.Message)`).
Whether that repetition earns static factory helpers on `DiscordAlert` itself is deferred to the
Open Questions below, once the real call sites exist to judge it by.

### HTTP client registration

No plain outbound `HttpClient` exists in `Neba.Api` today (the only precedent,
`AddRefitGeneratedClient` + `AddStandardResilienceHandler` in
`Neba.Website.Server/Services/ApiServicesConfiguration.cs`, is the website calling this API, not
this API calling out). Discord's webhook API is a single `POST` with a JSON body — no need for
Refit's generated-client machinery. Register a typed client instead:

```csharp
// src/Neba.Api/Discord/DiscordConfiguration.cs
internal static class DiscordConfiguration
{
    internal static WebApplicationBuilder AddDiscord(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<DiscordSettings>()
            .Bind(builder.Configuration.GetSection(DiscordSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHttpClient<IDiscordNotifier, DiscordNotifier>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<DiscordSettings>>().Value;
                client.BaseAddress = new Uri(settings.WebhookUrl);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.Retry.MaxRetryAttempts = 2;
            });

        return builder;
    }
}
```

Called from `InfrastructureConfiguration.AddInfrastructure()` alongside `.AddEmail()`. Short
timeouts and a small retry budget — this must never become the slow part of a request or job.
`Microsoft.Extensions.Http.Resilience` is already a package dependency (used by the website
project), so no new package.

### Config / secrets

Follows the `EmailSettings` pattern (`Configure<T>` + flattened singleton), not the
`HangfireSettings` pattern — there's exactly one value:

```csharp
// src/Neba.Api/Discord/DiscordSettings.cs
internal sealed class DiscordSettings
{
    public const string SectionName = "Discord";

    [Required]
    public string WebhookUrl { get; init; } = string.Empty;
}
```

`appsettings.json`:
```json
"Discord": {
  "WebhookUrl": ""
}
```

Production value comes from Key Vault, per ADR-0002's `--`-for-`:` convention: config key
`Discord:WebhookUrl` → Key Vault secret `Discord--WebhookUrl`, seeded via the existing
`az keyvault secret set` step in `.github/workflows/cd.yml`. Locally, set it via User Secrets or
`appsettings.Development.json` (Development already has direct config overrides for things like
`Legacy:ApiKey`).

### Never-throw behavior

`DiscordNotifier.NotifyAsync` wraps the HTTP call in try/catch, same shape as
`ResilientAuditDataProvider`: log a Warning via `[LoggerMessage]` on failure (non-2xx response or
thrown exception) and return without rethrowing. `#pragma warning disable CA1031` with the same
kind of rationale comment ("a Discord outage degrades to a logged warning instead of failing the
caller's operation").

```csharp
// src/Neba.Api/Discord/DiscordNotifier.cs
#pragma warning disable CA1031 // a Discord outage must degrade to a logged warning, never fail the caller's operation
internal sealed class DiscordNotifier(HttpClient httpClient, ILogger<DiscordNotifier> logger) : IDiscordNotifier
{
    private const int CriticalColor = 0xE74C3C; // red
    private const int WarningColor = 0xF1C40F;  // yellow

    public async Task NotifyAsync(DiscordAlert alert, CancellationToken cancellationToken)
    {
        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = alert.Title,
                    description = alert.Description,
                    color = alert.Severity == DiscordAlertSeverity.Critical ? CriticalColor : WarningColor,
                    fields = alert.Fields?.Select(field => new { name = field.Key, value = field.Value, inline = true }),
                    timestamp = DateTimeOffset.UtcNow
                }
            }
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(string.Empty, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDiscordPostRejected(alert.Title, (int)response.StatusCode);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogDiscordPostFailed(alert.Title, exception);
        }
    }
}
#pragma warning restore CA1031

internal static partial class DiscordNotifierLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Discord webhook rejected alert '{Title}' with status {StatusCode}.")]
    public static partial void LogDiscordPostRejected(this ILogger<DiscordNotifier> logger, string title, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to post Discord alert '{Title}'.")]
    public static partial void LogDiscordPostFailed(this ILogger<DiscordNotifier> logger, string title, Exception exception);
}
```

Two distinct failure paths, both swallowed: a thrown exception (network/DNS/timeout — the
resilience handler has already exhausted its retries by the time this catch runs) and a non-2xx
response that didn't throw (e.g. Discord returns 401 for a revoked webhook, or 429 if the retry
handler's backoff wasn't enough to clear a rate limit) — logged separately since the two point at
different root causes (webhook misconfigured vs. transient outage).

### Message format

Discord webhooks accept a JSON body with an `embeds` array — richer than plain text and lets
severity map to an accent color (Discord embed `color` is a decimal RGB int):

| `DiscordAlert.Severity` | Embed color | Used for |
|---|---|---|
| `Critical` | red | pipeline silently stopped, notification system itself failing |
| `Warning`  | yellow | data anomaly needing manual review, degraded-but-continuing |

Body: title, short description, `fields` for structured context (e.g. `SeasonId`, `TournamentId`,
`LegacyId`) so the on-call person doesn't have to jump to logs just to know what to look up.
Timestamps use the embed's built-in `timestamp` field (Discord renders it in the viewer's local
time).

### Testing

`tests/Neba.Api.Tests/Discord/DiscordNotifierTests.cs`, namespace `Neba.Api.Tests.Discord`,
`[UnitTest]` + `[Component("Discord")]`. No local Discord emulator exists, so — unlike
`GoogleWorkspaceEmailSenderTests`'s Mailpit fixture — this stubs the `HttpMessageHandler` (e.g. a
`DelegatingHandler` test double or `Microsoft.Extensions.Http.Resilience`-compatible stub) to
assert on the outgoing request body/URL and to prove a non-2xx / thrown exception is swallowed
and logged rather than propagated. Check `tests/Neba.Api.Tests/Legacy` and
`tests/Neba.Api.Tests/Documents` first in case a reusable `HttpMessageHandler` stub already
exists before writing a new one.

### Open questions for implementation time

- Confirm the resilience-handler timeout/retry numbers above against what Discord's rate limits
  actually tolerate (Discord webhooks are rate-limited per-webhook; a retry storm during an
  incident could make things worse, not better).
- Decide whether `DiscordAlert` construction helpers live next to each call site or as static
  factory methods on `DiscordAlert` itself, once Phase 2's call sites are actually built and the
  repetition (or lack of it) is visible.

---

## Phase 2 — Candidate events

Ordered by how strong a case each is, not by where it sits in the codebase. "Why" explains why it
would otherwise go unnoticed; "Post" is what the alert should say; "Method" shows the call site
with the Discord wiring added and whether it needs a `CA1031` suppression.

**On `CA1031`**: only two candidates (#1, #4) introduce a genuinely new `catch (Exception)` —
everywhere else either reuses an existing, already-suppressed catch (#2), isn't a catch at all
(#3 receives an already-caught exception; #5/#6 are plain conditionals/loops; #7 reads
`context.Exception`, which Hangfire already caught). For #1 and #4, catching broadly and treating
every exception identically (post to Discord, then rethrow) is the right call, not a shortcut —
it matches the existing precedent in this codebase (`ResilientAuditDataProvider`,
`DeleteSponsorFilesJobHandler`, `DeleteArticleFilesJobHandler`, `DeleteTournamentFilesJobHandler`)
of catching `Exception` with a `[SuppressMessage("Design", "CA1031:...", Justification = "...")]`
on the method, each with a one-line reason. Use the method-level attribute form, not a file-level
`#pragma` — it scopes the suppression to exactly the method that needs it and keeps the
justification next to the code, whereas a `#pragma` silently covers everything below it in the
file. (`ResilientAuditDataProvider` predates this convention and still uses the file-level
`#pragma` form — not a pattern to copy for new code.)

### 1. Email sender failures — `src/Neba.Api/Email/GoogleWorkspaceEmailSender.cs`

**Why**: This is the compounding case. `GoogleWorkspaceEmailSender.SendAsync` has no try/catch at
all today — SMTP failures propagate as unhandled exceptions to the caller. Every caller is a
legacy-sync Hangfire job (`CompleteSeasonSyncJob`, `CompleteTournamentSyncJob`,
`SyncTournamentResultsJob`, `SyncSquadScoresSyncJob`, `NewTournamentSyncJob`), and those emails
*are* the existing mechanism for telling a human "this legacy sync needs manual attention." If
SMTP is down, the alert that was supposed to say "look at this" never arrives, and there is
currently no fallback and no second signal. Wiring Discord here means the alerting mechanism has
a backup that doesn't share a failure mode with email.

**Post**: `Critical` — "Email delivery failed" with the intended recipient, subject, and the
underlying exception message. This one **can't** just swallow-and-log the way other candidates
do, because the whole point is surfacing what would otherwise be lost — so it should fire even
though the SMTP failure itself will already be logged.

**Method**: `SendAsync` has no try/catch today, so this is a genuinely new broad catch —
`[SuppressMessage("Design", "CA1031:Do not catch general exception types", ...)]` needed, same
convention as `DeleteSponsorFilesJobHandler`/`DeleteArticleFilesJobHandler`. Critically, the catch
must **rethrow** after notifying: today an SMTP failure propagates unhandled and fails the calling
Hangfire job, which is the only reason a human currently finds out (via the job's Failed state).
Swallowing it here — even with the Discord post — would make the job report Success while the
email silently never went out, which is a net loss of visibility if the Discord post itself also
fails (webhook down, network blip). Discord is meant to be a second channel, not a replacement for
the first.

```csharp
internal sealed class GoogleWorkspaceEmailSender(
    EmailSettings emailSettings,
    IDiscordNotifier discordNotifier,
    ILogger<GoogleWorkspaceEmailSender> logger)
        : IEmailSender
{
    private readonly EmailSettings _settings = emailSettings;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "SMTP failures must post to Discord before the job still fails visibly; every failure mode gets identical treatment.")]
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var mimeMessage = new MimeMessage();
        // ... build mimeMessage as today ...

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.TlsMode, cancellationToken);
            if (!string.IsNullOrEmpty(_settings.UserName))
                await client.AuthenticateAsync(_settings.UserName, _settings.AppPassword, cancellationToken);
            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            logger.LogEmailSent(message.To, message.Subject);
        }
        catch (Exception exception)
        {
            await discordNotifier.NotifyAsync(
                new DiscordAlert(
                    DiscordAlertSeverity.Critical,
                    "Email delivery failed",
                    exception.Message,
                    new Dictionary<string, string>
                    {
                        ["Recipient"] = message.To,
                        ["Subject"] = message.Subject
                    }),
                cancellationToken);

            throw;
        }
    }
}
```

> **Note (found while writing this plan)**: the working tree already has an uncommitted, partial
> version of this change (`git diff -- src/Neba.Api/Email/GoogleWorkspaceEmailSender.cs`) with two
> bugs to fix when this is picked up: a bare `[SuppressMessage]` with no `category`/`checkId`
> arguments (won't compile — `SuppressMessageAttribute` requires both), and the catch block does
> not rethrow, which is the exact "job reports Success while the email silently vanishes" hazard
> described above.

### 2. `ResilientAuditDataProvider` swallowed failures — `src/Neba.Api/Auditing/ResilientAuditDataProvider.cs`

**Why**: By design, an Azure Table Storage outage here degrades to a logged Warning and the
audited operation still succeeds — correct, so users aren't blocked by an audit-storage blip. But
that also means compliance/audit data is silently being lost with the *only* signal being a log
line nobody is watching. This is the audit equivalent of #1: the safety net's own failure has no
notification.

**Post**: `Warning` — "Audit event write failed" with the audit event type and the exception. Low
volume expected (this should only fire during a real storage outage), so it's safe to post every
occurrence rather than debouncing.

**Method**: the catch blocks already exist and are already covered by the file-level
`#pragma warning disable CA1031` — no new suppression needed, just add the notifier call
alongside the existing log call in each of the four overrides. Shown for `InsertEventAsync`; the
same one-line addition applies to `InsertEvent`, `ReplaceEvent`, and `ReplaceEventAsync`.

```csharp
internal sealed class ResilientAuditDataProvider(
    IAuditDataProvider inner,
    IDiscordNotifier discordNotifier,
    ILogger<ResilientAuditDataProvider> logger)
        : AuditDataProvider
{
    public override async Task<object?> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            return await inner.InsertEventAsync(auditEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogAuditEventInsertFailed(exception);

            await discordNotifier.NotifyAsync(
                new DiscordAlert(
                    DiscordAlertSeverity.Warning,
                    "Audit event write failed",
                    exception.Message,
                    new Dictionary<string, string>
                    {
                        ["EventType"] = auditEvent.EventType ?? "unknown"
                    }),
                cancellationToken);

            return null;
        }
    }
}
```

### 3. `GlobalExceptionHandler` — `src/Neba.Api/ErrorHandling/GlobalExceptionHandler.cs`

**Why**: Every unhandled exception across the entire API funnels through this one place today,
logged and turned into a generic 500. It's the single highest-leverage wire in the app — one call
site covers "something broke that nobody wrote a specific handler for," which is exactly the
category most likely to be a real bug rather than an expected failure mode.

**Post**: `Critical` — exception type, message, and the request path. Consider a lightweight
debounce (e.g. don't re-post the same exception type/path combo more than once per N minutes) so
a single misbehaving endpoint under retry doesn't flood the channel — this is the one candidate
with real flood risk, since it's tied to request volume rather than a scheduled/legacy event.

**Method**: `TryHandleAsync` doesn't introduce a new catch block — `exception` arrives already
caught by the ASP.NET Core exception-handling middleware, which is the whole point of
`IExceptionHandler`. No CA1031 concern here; it's just one more call alongside the existing log
call.

```csharp
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IDiscordNotifier discordNotifier,
    ILogger<GlobalExceptionHandler> logger)
        : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogException(exception);

        await discordNotifier.NotifyAsync(
            new DiscordAlert(
                DiscordAlertSeverity.Critical,
                "Unhandled exception",
                exception.Message,
                new Dictionary<string, string>
                {
                    ["ExceptionType"] = exception.GetType().Name,
                    ["RequestPath"] = httpContext.Request.Path
                }),
            cancellationToken);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unhandled exception occurred while processing the request."
            }
        });
    }
}
```

### 4. Legacy sync bridge health canary — `src/Neba.Api/Legacy/Ping.cs` (`PongJob`)

**Why**: A legacy-triggered self-check that already rethrows on failure so Hangfire marks the job
Failed. A failure here likely means the entire nebamgmt-v3 ↔ website bridge is unreachable —
every other legacy sync job (bowler/tournament/season/results/stats/awards) is downstream of the
same bridge being up. High-value, low-volume: this should almost never fire, which is exactly
what makes it worth watching closely when it does.

**Post**: `Critical` — "Legacy bridge ping failed" with the exception. No structured fields
needed beyond that; the whole point is "check if the bridge is down."

**Method**: the existing catch is `catch (Exception ex) when (ex is HttpRequestException or
TaskCanceledException)` — narrow on purpose, presumably to only treat transport failures as the
"bridge is down" signal. But the non-2xx-response path (`throw new InvalidOperationException(...)`
a few lines above) is just as much a "bridge is down" symptom and currently falls through that
`when` filter uncaught — same failure, no alert. Widening to catch `Exception` closes that gap and
needs `[SuppressMessage(..., CA1031, ...)]`; the alternative (posting inline at the
`InvalidOperationException` throw site, keeping the narrow catch for the other two) avoids the
suppression but duplicates the alert-construction logic across two spots for what's really one
signal ("the ping failed"). The single broadened catch is the simpler one.

```csharp
internal sealed class PongJob(
    IHttpClientFactory httpClientFactory,
    IServer server,
    IDiscordNotifier discordNotifier,
    ILogger<PongJob> logger)
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Any failure reaching /health means the legacy bridge is down; every cause gets the same Discord alert before the job still fails visibly.")]
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
        }
        catch (Exception ex)
        {
            logger.LogPongFailed(ex.Message);

            await discordNotifier.NotifyAsync(
                new DiscordAlert(DiscordAlertSeverity.Critical, "Legacy bridge ping failed", ex.Message),
                CancellationToken.None);

            throw;
        }
    }
}
```

Note the notifier call passes `CancellationToken.None`, not `ct` — `ct` is what the job itself is
running under, and a `TaskCanceledException`/timeout is one of the exact failure modes this alert
exists to report. Using `ct` for the Discord post would make that report race against the same
cancellation that triggered it. Same reasoning applies to the other catch-block call sites above
(#1, #2, #3) — worth using `CancellationToken.None` there too rather than the ambient token, since
in each case the notifier is reporting the failure of the operation the token belongs to.

### 5. Season/tournament pipeline dead-ends — `src/Neba.Api/Legacy/Seasons/Complete/CompleteSeasonSyncJob.cs`, `src/Neba.Api/Legacy/Tournaments/Complete/CompleteTournamentSyncJob.cs`

**Why**: Both jobs have an early-return path when the legacy season/tournament can't be matched
to a website record (`CompleteSeasonSyncJob.cs:47-75`, `CompleteTournamentSyncJob.cs:29-46`).
Today that sends an email (`UnknownLegacySeasonEmail`/`UnmatchedSeasonEmail`,
`UnlinkedTournamentCompletionEmail`) and stops — meaning an entire season's award pipeline, or an
entire tournament's results/stats pipeline, silently never runs. Nothing else in the UI tells
anyone this happened; it looks like the season/tournament is just "not done yet" rather than
"stuck forever." This is the highest-severity case among the legacy-sync jobs because the blast
radius is an entire season or tournament, not one row.

**Post**: `Critical` — "Season/tournament completion could not be matched" with the legacy id,
season/tournament name if resolvable, and which email was sent (so the on-call person knows a
second copy already went to inboxes). Same underlying event as the email in #1 — this is a
direct, redundant channel for it, not a replacement.

**Method**: both early-return branches are plain conditionals, not exceptions — no catch block, no
CA1031 question. `CompleteSeasonSyncJob` has two such branches (unknown legacy season, unmatched
website season); shown for the first.

```csharp
internal sealed class CompleteSeasonSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IBackgroundJobClient jobs,
    IFusionCache cache,
    IEmailSender emailSender,
    IDiscordNotifier discordNotifier,
    ILogger<CompleteSeasonSyncJob> logger)
{
    public async Task SyncAsync(int legacySeasonId, CancellationToken ct)
    {
        // ... legacySeason lookup as today ...

        if (legacySeason is null)
        {
            logger.LogLegacySeasonNotFound(legacySeasonId);

            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: season completion for unknown legacy season",
                HtmlBody = new UnknownLegacySeasonEmail(legacySeasonId).ToHtmlBody()
            }, ct);

            await discordNotifier.NotifyAsync(
                new DiscordAlert(
                    DiscordAlertSeverity.Critical,
                    "Season completion could not be matched",
                    "No website season could be matched to the completed legacy season.",
                    new Dictionary<string, string>
                    {
                        ["LegacySeasonId"] = legacySeasonId.ToString(CultureInfo.InvariantCulture),
                        ["EmailSent"] = nameof(UnknownLegacySeasonEmail)
                    }),
                ct);

            return;
        }

        // ... second branch (unmatched website season) gets the same two-call pattern,
        // using UnmatchedSeasonEmail / SeasonName + start/end dates as fields ...
    }
}
```

(`CompleteTournamentSyncJob`'s version is the same shape with `LegacyTournamentId`/`TournamentName`
and `UnlinkedTournamentCompletionEmail`.)

### 6. Duplicate legacy result rows — `src/Neba.Api/Legacy/Tournaments/Complete/SyncTournamentResultsJob.cs:128-136`

**Why**: The one Error-level log in the entire legacy-sync chain (`LogLegacyBowlerHasMultipleResultRows`).
Every other anomaly in this file degrades to skip-and-email; this one is already treated as more
serious in the code, which is a decent signal it should also be the more serious severity here.
Indicates a genuine data-integrity anomaly in the legacy database, not just an unmapped/pending
record — worth a human looking at the legacy row directly.

**Post**: `Warning` — bowler id, tournament id, and the duplicate row count.

**Method**: this is a `foreach` over an already-computed grouping, not a catch — no CA1031
question. The existing `LogLegacyBowlerHasMultipleResultRows` call gets a sibling notifier call
right next to it.

```csharp
var resultRowsByBowlerId = resultRows.GroupBy(r => r.BowlerId).ToList();
var anomalousLegacyBowlerIds = resultRowsByBowlerId.Where(g => g.Count() > 1).Select(g => g.Key).ToList();

foreach (var legacyBowlerId in anomalousLegacyBowlerIds)
{
    logger.LogLegacyBowlerHasMultipleResultRows(legacyBowlerId, legacyTournamentId);

    var rowCount = resultRowsByBowlerId.Single(g => g.Key == legacyBowlerId).Count();

    await discordNotifier.NotifyAsync(
        new DiscordAlert(
            DiscordAlertSeverity.Warning,
            "Duplicate legacy result rows",
            "A bowler has more than one result row for this tournament in the legacy database.",
            new Dictionary<string, string>
            {
                ["LegacyBowlerId"] = legacyBowlerId.ToString(CultureInfo.InvariantCulture),
                ["LegacyTournamentId"] = legacyTournamentId.ToString(CultureInfo.InvariantCulture),
                ["RowCount"] = rowCount.ToString(CultureInfo.InvariantCulture)
            }),
        ct);
}
```

### 7. Recurring job misses with delayed visibility — `create-next-season` (`SeasonsConfiguration.cs`), `sync-document-*` (`DocumentsConfiguration.cs`)

**Why**: Both are scheduled (quarterly / weekly) rather than triggered by a user action, so a
failure has no natural moment where someone would notice — `create-next-season` failing means no
season exists when tournaments eventually need one, which wouldn't surface for weeks; a failed
`sync-document-*` run means a public document (bylaws, tournament rules) goes stale silently.
Lower urgency than 1–6, but included because "scheduled and unattended" is exactly the shape of
failure a dashboard-only signal is worst at catching.

**Post**: `Warning` — job name and exception. These already rethrow on failure
(`SyncDocumentToStorageJobHandler.cs:77-85`), so wiring this is a matter of catching at the
Hangfire job-filter level (or per-job) rather than each handler individually — worth doing as one
cross-cutting hook rather than six near-identical call sites. See Open Questions below.

**Method**: a new `IServerFilter`, same shape as the existing `HangfireConsoleServerFilter`
(`src/Neba.Api/BackgroundJobs/HangfireConsoleServerFilter.cs`) — registered once in
`BackgroundJobsConfiguration.AddHangfireInfrastructure`, not per-job. `OnPerformed` reads
`context.Exception`, which Hangfire has already caught by the time this runs — we introduce no
catch of our own, so no CA1031 question. `IDiscordNotifier.NotifyAsync` is async but
`IServerFilter` is sync, so it's called with `.GetAwaiter().GetResult()`; that's safe here because
Phase 1 guarantees `NotifyAsync` never throws.

```csharp
internal sealed class DiscordJobFailureFilter(IDiscordNotifier discordNotifier) : IServerFilter
{
    public void OnPerforming(PerformingContext context) { }

    public void OnPerformed(PerformedContext context)
    {
        if (context.Exception is null || context.ExceptionHandled)
        {
            return;
        }

        var alert = new DiscordAlert(
            DiscordAlertSeverity.Warning,
            "Recurring job failed",
            context.Exception.Message,
            new Dictionary<string, string>
            {
                ["JobName"] = context.BackgroundJob.Job.Method.Name
            });

        discordNotifier.NotifyAsync(alert, CancellationToken.None).GetAwaiter().GetResult();
    }
}
```

Registered alongside the other global filters:

```csharp
options.UseFilter(new AutomaticRetryAttribute { Attempts = settings.AutomaticRetryAttempts })
    .UseFilter(new HangfireJobExpirationFilterAttribute(settings))
    .UseFilter(new HangfireConsoleServerFilter())
    .UseFilter(new DiscordJobFailureFilter(serviceProvider.GetRequiredService<IDiscordNotifier>()))
    // ...
```

`context.ExceptionHandled` guards against re-alerting a job whose failure some other filter (e.g.
`AutomaticRetryAttribute` mid-retry) has already converted into a non-failure outcome — only the
final, truly-failed attempt should post. This filter would fire for *every* job that exhausts
retries, not just `create-next-season`/`sync-document-*` — worth deciding at implementation time
whether that's desirable (a blanket safety net for any future job) or too broad (see Open Question
below on scoping it).

### Deliberately excluded

- **Domain audit events** (Bowler/Season/Tournament/award entity changes) — routine activity,
  not failures.
- **`CachedQueryHandlerDecorator` deserialization catches** — falls back to source-of-truth by
  design; not user-visible, not actionable.
- **Tracing decorators** (`TracedCommandHandlerDecorator`, `TracedQueryHandlerDecorator`) —
  observability plumbing, rethrow after tagging; the exception is already visible wherever it
  ends up (likely #3).
- **Per-file blob cleanup jobs** (`DeleteSponsorFilesJobHandler`, etc.) — catch-log-continue by
  design, orphaned storage files are a cost concern, not urgent.
- **Per-award-winner failures within the 8 `Assign*AwardJob` jobs** — considered but left out of
  the initial wire-up; lower blast radius than #5 (one winner, not a whole season), and the
  existing per-failure log already exists. Revisit if these turn out to need faster visibility in
  practice.

### Open question for implementation time

Several candidates (#5, #6, #7) sit inside Hangfire jobs that already have
`[AuditJobExecutionFilter]` applied. Worth checking whether a single Hangfire job filter that
posts to Discord on job failure (reading `PerformContext.Items`/`context.Exception`) covers #7
and part of #5/#6 more cheaply than instrumenting each handler — versus the risk (noted in
CLAUDE.md's Hangfire learnings) of two filter instances stepping on each other's `PerformContext.Items`
keys. If a global filter is used, it must exclude the "should this even count as failure-worth-Discord"
judgment calls that are specific to each job (e.g. an unmatched season vs. a transient DB
timeout) — those still need per-job logic, so a global filter would likely only cover the
`Critical`-vs-nothing case (job exhausted retries), with #5/#6's more specific messaging staying
inline in the handler.
