# Oil Pattern Discord Announcement

When a tournament's oil pattern reveal time passes, post to the Discord news channel that the pattern is announced, with a link to the tournament details page. GitHub issue #180.

## Decisions locked in during scoping

- **Not critical.** No NFR checkpoint. A missed post raises the existing job-failure Discord alert; staff post by hand.
- **Permissions:** no new endpoint, policy or role. The job is scheduled from the existing create and edit tournament handlers, so existing tournament policies already gate it.
- **No UI surface.** No flow sketch, no mockups. Phase 2 states this.
- **Content:** the sentence `The oil pattern for {Tournament Name} has been announced.` and the details link only. No pattern name, length, ratio or dates. The pattern appears on the website through the existing `EvictOilPatternRevealCacheJob`, not through the Discord post.
- **Format:** an embed, as news uses. Title is the sentence, linked to `{WebsiteSettings.BaseUrl}/tournaments/{Id}`. Image is the tournament's display logo (own logo, else title sponsor's, via `TournamentLogoResolver`) when there is one. Sent to the news webhook through `INewsAnnouncementNotifier` (no-retry client), reused as is.
- **Delay:** configurable, 120 seconds by default and 10 in development, after `OilPatternRevealDateTime`. Its own setting, `OilPatternAnnouncement:DelaySeconds`.
- **"Has an oil pattern":** at least one linked `TournamentOilPattern`. Length and ratio categories alone do not count. No catch-up post when a pattern is added later.
- **Migration backfill:** existing tournaments with a reveal date get `oil_pattern_announced_at_utc = oil_pattern_reveal_date_utc`, so they never announce.
- **No cache eviction in the job.** The claim writes only `oil_pattern_announced_at_utc`, which no cached tournament DTO exposes (unlike the news article's `AnnouncedAtUtc`). The job therefore does not take `IFusionCache`.
- **Rules from the issue that stay:** schedule only when the reveal date is in the future at save time; at most one post per tournament, ever (claim before post, never undo, never retry); no job cancelling; stale jobs exit as no-ops; every skip logs at Information.

## Phase 1: API

Mirrors `Features/News/AnnounceArticleToDiscord` from #232. No new endpoint, route or Contracts type.

### Domain

`src/Neba.Api/Features/Tournaments/Domain/Tournament.cs`, next to `OilPatternRevealDateTime`:

```csharp
/// <summary>
/// When the oil pattern reveal was announced, or <see langword="null"/> if it has not been. Set once.
/// </summary>
public DateTimeOffset? OilPatternAnnouncedAtUtc { get; private set; }

/// <summary>
/// When the oil pattern reveal should be announced: <paramref name="delay"/> after the reveal time, so the
/// page shows the pattern before the link is clicked. <see langword="null"/> when there is no reveal time,
/// it has passed, or the reveal was already announced.
/// </summary>
public DateTimeOffset? GetOilPatternAnnouncementTime(DateTimeOffset now, TimeSpan delay)
{
    if (OilPatternRevealDateTime is not { } revealAt
        || revealAt <= now
        || OilPatternAnnouncedAtUtc is not null)
    {
        return null;
    }

    return revealAt + delay;
}

/// <summary>
/// Records that the oil pattern reveal was announced. A second call keeps the first timestamp.
/// </summary>
public void MarkOilPatternAnnounced(DateTimeOffset now)
    => OilPatternAnnouncedAtUtc ??= now;
```

### Application

`Features/Tournaments/AnnounceOilPatternToDiscord/AnnounceOilPatternToDiscordJob.cs`:

```csharp
using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;

/// <summary>
/// Posts the oil pattern reveal to the Discord news channel. Never retried: a retry after a timeout could post twice.
/// </summary>
[NoAutomaticRetry]
internal sealed record AnnounceOilPatternToDiscordJob
    : IBackgroundJob
{
    public required TournamentId TournamentId { get; init; }

    /// <summary>
    /// The reveal time this job was scheduled for. The job exits if the tournament's time has since changed.
    /// </summary>
    public required DateTimeOffset ExpectedRevealDateTime { get; init; }

    public string JobName
        => $"{nameof(AnnounceOilPatternToDiscordJob)}: tournament {TournamentId}";
}
```

`.../AnnounceOilPatternToDiscordJobHandler.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Security;
using Neba.Api.Storage;

namespace Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;

internal sealed class AnnounceOilPatternToDiscordJobHandler(
    AppDbContext appDbContext,
    TimeProvider timeProvider,
    WebsiteSettings websiteSettings,
    IFileStorageService fileStorageService,
    INewsAnnouncementNotifier newsAnnouncementNotifier,
    ILogger<AnnounceOilPatternToDiscordJobHandler> logger)
    : IBackgroundJobHandler<AnnounceOilPatternToDiscordJob>
{
    public async Task ExecuteAsync(AnnounceOilPatternToDiscordJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var tournamentId = job.TournamentId.ToString();

        var row = await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournament.Id == job.TournamentId)
            .Select(tournament => new TournamentRow
            {
                Name = tournament.Name,
                RevealDateTime = tournament.OilPatternRevealDateTime,
                AnnouncedAtUtc = tournament.OilPatternAnnouncedAtUtc,
                HasOilPattern = tournament.OilPatterns.Any(),
                LogoContainer = tournament.Logo != null ? tournament.Logo.Container : null,
                LogoPath = tournament.Logo != null ? tournament.Logo.Path : null,
                Sponsors = tournament.Sponsors
                    .Select(tournamentSponsor => new SponsorLogoRow
                    {
                        TitleSponsor = tournamentSponsor.TitleSponsor,
                        LogoContainer = tournamentSponsor.Sponsor.Logo != null ? tournamentSponsor.Sponsor.Logo.Container : null,
                        LogoPath = tournamentSponsor.Sponsor.Logo != null ? tournamentSponsor.Sponsor.Logo.Path : null
                    }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            logger.LogAnnouncementSkippedMissing(tournamentId);
            return;
        }

        if (row.RevealDateTime is not { } revealDateTime)
        {
            logger.LogAnnouncementSkippedNoRevealDate(tournamentId);
            return;
        }

        if (!IsSameInstant(revealDateTime, job.ExpectedRevealDateTime))
        {
            logger.LogAnnouncementSkippedStaleDate(tournamentId, job.ExpectedRevealDateTime, revealDateTime);
            return;
        }

        if (!row.HasOilPattern)
        {
            logger.LogAnnouncementSkippedNoPattern(tournamentId);
            return;
        }

        if (row.AnnouncedAtUtc is not null)
        {
            logger.LogAnnouncementSkippedAlreadyAnnounced(tournamentId);
            return;
        }

        // Claim before posting, in one atomic statement, so concurrent jobs produce one post. The WHERE
        // clause repeats the checks above, so an edit between the read and this statement still blocks
        // the post. Never undone: a failed post is not retried, because a timeout after Discord accepted
        // it looks the same as a failure. Tournament.MarkOilPatternAnnounced is the same rule in memory.
        var revealFloor = job.ExpectedRevealDateTime - RevealDateTolerance;
        var revealCeiling = job.ExpectedRevealDateTime + RevealDateTolerance;

        var claimed = await appDbContext.Tournaments
            .Where(tournament => tournament.Id == job.TournamentId
                && tournament.OilPatternAnnouncedAtUtc == null
                && tournament.OilPatterns.Any()
                && tournament.OilPatternRevealDateTime >= revealFloor
                && tournament.OilPatternRevealDateTime <= revealCeiling)
            .ExecuteUpdateAsync(
                set => set.SetProperty(tournament => tournament.OilPatternAnnouncedAtUtc, timeProvider.GetUtcNow()),
                cancellationToken);

        if (claimed == 0)
        {
            logger.LogAnnouncementSkippedClaimLost(tournamentId);
            return;
        }

        var (logoContainer, logoPath) = TournamentLogoResolver.Resolve(
            row.LogoContainer,
            row.LogoPath,
            row.Sponsors.Select(s => (s.TitleSponsor, s.LogoContainer, s.LogoPath)));

        Uri? logoUrl = logoContainer is not null && logoPath is not null
            ? fileStorageService.GetBlobUri(logoContainer, logoPath)
            : null;

        await newsAnnouncementNotifier.NotifyAsync(
            OilPatternAnnouncementMessage.Create(row.Name, tournamentId, websiteSettings.BaseUrl, logoUrl),
            cancellationToken);

        logger.LogOilPatternAnnounced(tournamentId);
    }

    // Absorbs sub-millisecond rounding between the time the job was scheduled with and the stored one.
    // A real edit moves the time by far more than this.
    private static readonly TimeSpan RevealDateTolerance = TimeSpan.FromMilliseconds(1);

    private static bool IsSameInstant(DateTimeOffset stored, DateTimeOffset expected)
        => (stored - expected).Duration() <= RevealDateTolerance;

    private sealed class TournamentRow
    {
        public required string Name { get; init; }
        public DateTimeOffset? RevealDateTime { get; init; }
        public DateTimeOffset? AnnouncedAtUtc { get; init; }
        public required bool HasOilPattern { get; init; }
        public string? LogoContainer { get; init; }
        public string? LogoPath { get; init; }
        public required List<SponsorLogoRow> Sponsors { get; init; }
    }

    private sealed class SponsorLogoRow
    {
        public required bool TitleSponsor { get; init; }
        public string? LogoContainer { get; init; }
        public string? LogoPath { get; init; }
    }
}

internal static partial class AnnounceOilPatternToDiscordJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: it no longer exists.")]
    public static partial void LogAnnouncementSkippedMissing(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: it has no reveal date.")]
    public static partial void LogAnnouncementSkippedNoRevealDate(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: the job was scheduled for {ExpectedRevealDateTime} but the reveal date is now {RevealDateTime}.")]
    public static partial void LogAnnouncementSkippedStaleDate(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId, DateTimeOffset expectedRevealDateTime, DateTimeOffset revealDateTime);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: no oil pattern is assigned.")]
    public static partial void LogAnnouncementSkippedNoPattern(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: it was already announced.")]
    public static partial void LogAnnouncementSkippedAlreadyAnnounced(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped announcing the oil pattern for tournament {TournamentId}: another job claimed it, or it changed after the check.")]
    public static partial void LogAnnouncementSkippedClaimLost(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Announced the oil pattern for tournament {TournamentId} to Discord.")]
    public static partial void LogOilPatternAnnounced(this ILogger<AnnounceOilPatternToDiscordJobHandler> logger, string tournamentId);
}
```

`.../OilPatternAnnouncementScheduling.cs`:

```csharp
using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;

internal static class OilPatternAnnouncementScheduling
{
    // PostgreSQL stores timestamps to the microsecond, but a reveal time saved from the API can carry
    // finer ticks. The job compares the time it was scheduled for with the stored one, so schedule with
    // the value the database will return.
    private const long TicksPerMicrosecond = 10;

    extension(IBackgroundJobScheduler scheduler)
    {
        /// <summary>
        /// Schedules the announcement job when <paramref name="tournament"/> is due one. Does nothing when
        /// the reveal time is missing or past, or the reveal was already announced.
        /// </summary>
        public void ScheduleOilPatternAnnouncement(Tournament tournament, DateTimeOffset now, TimeSpan delay)
        {
            ArgumentNullException.ThrowIfNull(tournament);

            var announceAt = tournament.GetOilPatternAnnouncementTime(now, delay);

            if (announceAt is null)
            {
                return;
            }

            scheduler.Schedule(
                new AnnounceOilPatternToDiscordJob
                {
                    TournamentId = tournament.Id,
                    ExpectedRevealDateTime = TruncateToMicroseconds(tournament.OilPatternRevealDateTime!.Value)
                },
                announceAt.Value);
        }
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        => new(value.Ticks - (value.Ticks % TicksPerMicrosecond), value.Offset);
}
```

`Features/Tournaments/OilPatternAnnouncementSettings.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace Neba.Api.Features.Tournaments;

internal sealed class OilPatternAnnouncementSettings
{
    public const string SectionName = "OilPatternAnnouncement";

    /// <summary>
    /// Seconds between the oil pattern reveal and its announcement, so the page shows the pattern when the
    /// link is clicked. 120 in production; shortened in development.
    /// </summary>
    [Range(0, 3600)]
    public int DelaySeconds { get; init; } = 120;

    public TimeSpan Delay
        => TimeSpan.FromSeconds(DelaySeconds);
}
```

`Features/Tournaments/TournamentsConfiguration.cs` (new; there is none today), registered in `InfrastructureConfiguration.cs` after `AddNews`:

```csharp
namespace Neba.Api.Features.Tournaments;

internal static class TournamentsConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTournaments(IConfiguration configuration)
        {
            services.AddOptions<OilPatternAnnouncementSettings>()
                .Bind(configuration.GetSection(OilPatternAnnouncementSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
```

```csharp
// InfrastructureConfiguration.cs
builder.Services.AddNews(builder.Configuration);
builder.Services.AddTournaments(builder.Configuration);
```

`CreateTournamentCommandHandler.cs`: add `IOptions<OilPatternAnnouncementSettings> announcementSettings` to the primary constructor and replace the final block:

```csharp
        if (command.OilPatternRevealDateTime is { } revealAt && revealAt > timeProvider.GetUtcNow())
        {
            jobScheduler.Schedule(
                new EvictOilPatternRevealCacheJob { TournamentId = tournament.Id, SeasonId = season.Id },
                revealAt);
        }

        jobScheduler.ScheduleOilPatternAnnouncement(
            tournament, timeProvider.GetUtcNow(), announcementSettings.Value.Delay);
```

`EditTournamentCommandHandler.cs`: add the same constructor parameter, pass `tournament` to `ScheduleBackgroundJobs`, and add the call at its end (the reveal date is already on the saved tournament):

```csharp
        ScheduleBackgroundJobs(command, previousLogo, tournament, season.Id);
        ...
    private void ScheduleBackgroundJobs(
        EditTournamentCommand command, StoredFile? previousLogo, Tournament tournament, SeasonId seasonId)
    {
        TournamentLogoCleanup.ScheduleDeleteOfReplacedLogo(backgroundJobScheduler, previousLogo, command.Logo);

        if (command.OilPatternRevealDateTime is { } revealAt && revealAt > timeProvider.GetUtcNow())
        {
            backgroundJobScheduler.Schedule(
                new EvictOilPatternRevealCacheJob { TournamentId = tournament.Id, SeasonId = seasonId },
                revealAt);
        }

        backgroundJobScheduler.ScheduleOilPatternAnnouncement(
            tournament, timeProvider.GetUtcNow(), announcementSettings.Value.Delay);
    }
```

### Infrastructure

`Database/Configurations/TournamentConfiguration.cs`, after the reveal date mapping:

```csharp
builder.Property(tournament => tournament.OilPatternAnnouncedAtUtc)
    .HasColumnName("oil_pattern_announced_at_utc");
```

Migration `AddTournamentOilPatternAnnouncedAt` (generate with `dotnet ef migrations add`, then add the backfill; the table name is read from the snapshot):

```csharp
migrationBuilder.AddColumn<DateTimeOffset>(
    name: "oil_pattern_announced_at_utc",
    table: "tournaments",
    type: "timestamp with time zone",
    nullable: true);

// Tournaments that already have a reveal date must never announce.
migrationBuilder.Sql("""
    UPDATE tournaments
    SET oil_pattern_announced_at_utc = oil_pattern_reveal_date_utc
    WHERE oil_pattern_reveal_date_utc IS NOT NULL;
    """);
```

`Discord/OilPatternAnnouncementMessage.cs` (new, next to `NewsAnnouncement`):

```csharp
namespace Neba.Api.Discord;

internal static class OilPatternAnnouncementMessage
{
    /// <summary>
    /// Builds the announcement that a tournament's oil pattern is revealed: the sentence, linked to the
    /// tournament details page, with the tournament logo when there is one.
    /// </summary>
    public static NewsAnnouncement Create(string tournamentName, string tournamentId, string baseUrl, Uri? logoUrl)
        => new(
            $"The oil pattern for {tournamentName} has been announced.",
            new Uri($"{baseUrl.TrimEnd('/')}/tournaments/{tournamentId}"),
            logoUrl);
}
```

Settings files:

```json
// appsettings.json (after "NewsAnnouncement")
"OilPatternAnnouncement": {
  "DelaySeconds": 120
},

// appsettings.Development.json (after "NewsAnnouncement:DelaySeconds")
"OilPatternAnnouncement:DelaySeconds": 10,
```

### API / Contracts

- None. No route, request, response or policy change.

### Tests (`tests/Neba.Api.Tests`, `Component("Tournaments")`)

`tests/Neba.TestFactory/Tournaments/TournamentFactory.cs`: add `DateTimeOffset? oilPatternAnnouncedAtUtc = null` to `Create`, and after the sponsor and squad loops:

```csharp
if (oilPatternAnnouncedAtUtc is { } announcedAtUtc)
{
    tournament.MarkOilPatternAnnounced(announcedAtUtc);
}
```

`Bogus` leaves it `null`. Add `OilPatternAnnouncementMessageFactory` only if the builder tests need shared defaults; otherwise call the builder directly.

Test list (names are `DisplayName`s; all follow AAA comments, Shouldly, strict mocks, `FakeLogger<T>`):

- `TournamentOilPatternAnnouncementTests` (unit, domain)
  - `GetOilPatternAnnouncementTime_ShouldReturnNull_WhenNoRevealDate`
  - `..._WhenRevealDateIsPast` (also: equal to `now`)
  - `..._WhenAlreadyAnnounced` (including a reveal date moved later after the announcement)
  - `..._ShouldReturnRevealDatePlusDelay_WhenRevealDateIsFuture`
  - `MarkOilPatternAnnounced_ShouldKeepFirstTimestamp_WhenCalledTwice`
- `AnnounceOilPatternToDiscordJobHandlerTests` (integration with the test database, as the news handler tests)
  - One test per skip: missing, no reveal date, stale date, no pattern, already announced. Each asserts the notifier strict mock is never called and the Information log text.
  - Happy path: notifier receives the exact sentence, link and logo URL; log written; `OilPatternAnnouncedAtUtc` set.
  - Logo falls back to the title sponsor's logo; no logo gives a null image.
  - `ExecuteAsync_ShouldLeaveClaimSetAndRethrow_WhenPostFails` (the claim persists, no second notifier call).
  - `ExecuteAsync_ShouldPostOnce_WhenTwoJobsRunConcurrently`.
  - `ExecuteAsync_ShouldPostOnce_WhenDateMovedLaterAndBack`.
- `OilPatternAnnouncementSchedulingTests` (unit): schedules at reveal + delay with the truncated expected time; does nothing for null, past and announced tournaments.
- `CreateTournamentCommandHandlerTests` and `EditTournamentCommandHandlerTests` (edit): schedule or not for null, past, future and (edit only) announced reveal dates; the scheduled run time is reveal + delay. Add the new options parameter to the existing constructor calls.
- `OilPatternAnnouncementMessageTests` (unit): exact title and URL; trailing slash on the base URL; a different base URL (the preview domain) shows in the link; image only when given.
- `OilPatternAnnouncementSettingsTests` (unit, mirror `NewsAnnouncementSettingsTests`): default 120; `Delay` conversion; range validation.
- Migration integration check: a tournament row with a reveal date has `oil_pattern_announced_at_utc` equal to it after migration; a row without one has null.

### Deferred or out of scope

- Phase 2: nothing (no UI).
- Out of scope as in the issue: pattern details in the post, re-announcing, catch-up posts, retry, cancelling jobs, other channels, per-tournament toggle.

## Phase 2: UI

**No UI work.** The feature is a background job and a Discord post, so it adds no page, component, Refit method, or Playwright/bUnit test.

- **Existing screens are unchanged.** The create and edit tournament forms already have the reveal date field. Saving there schedules the job. There is no new field, label or toggle (a per-tournament skip toggle is out of scope).
- **API client.** `Neba.Api.Contracts` has no change, so there is nothing for Refit or the UI project to pick up.
- **Mock API.** No Contracts DTO gains a `required` property, so `tests/e2e/mock-api/mock-api-server.ts` needs no change.
- **Mockups and UI code draft.** Skipped, because there is no UI surface.

## Commit plan

Each commit builds and passes its own tests, in this order.

1. **Make the feature-plan skill commit in logical steps** — `.claude/skills/feature-plan/SKILL.md`. Adds the commit plan step and the rule to commit as each step is done.
2. **Plan the oil pattern Discord announcement** — `docs/plans/oil-pattern-discord-announcement.md`.
3. **Add OilPatternAnnouncedAtUtc to Tournament** — `Tournament.cs`, `TournamentConfiguration.cs`, the new migration with its designer and the model snapshot, `TournamentFactory.cs`, and the domain and migration tests. The migration backfills the reveal date so existing tournaments never announce.
4. **Add oil pattern announcement settings and message** — `OilPatternAnnouncementSettings.cs`, `TournamentsConfiguration.cs`, `InfrastructureConfiguration.cs`, both `appsettings` files, `OilPatternAnnouncementMessage.cs`, and their tests.
5. **Announce oil pattern reveals to Discord** — the job, handler and scheduling extension in `AnnounceOilPatternToDiscord/`, the job factory, and the job, handler and scheduling tests.
6. **Schedule the announcement when a tournament is saved** — `CreateTournamentCommandHandler.cs`, `EditTournamentCommandHandler.cs`, and their tests.
