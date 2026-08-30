using System.Data;
using System.Globalization;

using Dapper;

using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Email;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Seasons.Complete;

// Thin, two-stage on purpose: this job's only job is "resolve the website season by date-range
// match, then mark it complete." The award computation is scheduled an hour later - by then every
// tournament in the season should have its BowlerSeasonStats rows fully (re)computed by
// GenerateSeasonStatsJob's own ten-minute-after-tournament-completion delay. See the plan's
// Decision Recap for the full rationale on both the date-range match and the timing gap.
internal sealed class CompleteSeasonSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IBackgroundJobClient jobs,
    IFusionCache cache,
    IEmailSender emailSender,
    IDiscordNotifier discordNotifier,
    ILogger<CompleteSeasonSyncJob> logger)
{
    private static readonly TimeSpan AwardJobDelay = TimeSpan.FromHours(1);

    public async Task SyncAsync(int legacySeasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        // Aliased and unquoted, matching GenerateSeasonStatsJob's FetchSeasonTournamentsAsync
        // precedent for the same End-column-name-is-a-keyword issue: Postgres folds unquoted
        // identifiers to lowercase (matching this test suite's quoted "end" stand-in table), and
        // qualifying with the table alias avoids SQL Server's END keyword ambiguity without brackets.
        var legacySeason = await legacyConnection.QuerySingleOrDefaultAsync<LegacySeasonRow>(
            "SELECT s.Start, s.End FROM Season s WHERE s.Id = @SeasonId",
            new { SeasonId = legacySeasonId });
#pragma warning restore DAP005

        if (legacySeason is null)
        {
            logger.LogLegacySeasonNotFound(legacySeasonId);
            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: season completion for unknown legacy season",
                HtmlBody = new UnknownLegacySeasonEmail(legacySeasonId).ToHtmlBody()
            }, ct);

            var unknownSeasonAlert = new DiscordAlert(
                DiscordAlertSeverity.Critical,
                "Season completion could not be matched",
                "The legacy season completion event referenced a legacy season id that does not exist.",
                new Dictionary<string, string>
                {
                    ["LegacySeasonId"] = legacySeasonId.ToString(CultureInfo.InvariantCulture),
                    ["EmailSent"] = nameof(UnknownLegacySeasonEmail)
                });

            await discordNotifier.NotifyAsync(unknownSeasonAlert, ct);

            return;
        }

        var startDate = DateOnly.FromDateTime(legacySeason.Start);
        var endDate = DateOnly.FromDateTime(legacySeason.End);

        var season = await db.Seasons.SingleOrDefaultAsync(
            s => s.StartDate == startDate && s.EndDate == endDate, ct);

        if (season is null)
        {
            logger.LogLegacySeasonNotMatched(legacySeasonId, startDate, endDate);
            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: season completion with no matching website season",
                HtmlBody = new UnmatchedSeasonEmail(legacySeasonId, startDate, endDate).ToHtmlBody()
            }, ct);

            var unmatchedSeasonAlert = new DiscordAlert(
                DiscordAlertSeverity.Critical,
                "Season completion could not be matched",
                "The legacy season's date range does not match any website season.",
                new Dictionary<string, string>
                {
                    ["LegacySeasonId"] = legacySeasonId.ToString(CultureInfo.InvariantCulture),
                    ["StartDate"] = startDate.ToString("O", CultureInfo.InvariantCulture),
                    ["EndDate"] = endDate.ToString("O", CultureInfo.InvariantCulture),
                    ["EmailSent"] = nameof(UnmatchedSeasonEmail)
                });

            await discordNotifier.NotifyAsync(unmatchedSeasonAlert, ct);

            return;
        }

        var completeResult = season.CompleteSeason();
        if (completeResult.IsError)
        {
            // AlreadyComplete: expected on retry/re-fire. Not fatal — still schedule the award
            // jobs anyway; each is independently idempotent (see award job "already assigned"
            // guard), so there's no harm in re-scheduling them.
            logger.LogLegacySeasonAlreadyComplete(legacySeasonId, season.Id);
        }
        else
        {
            await db.SaveChangesAsync(ct);

            await cache.RemoveByTagAsync("neba:seasons", token: ct);
        }

        jobs.Schedule<AssignBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignWomanOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignSeniorBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignSuperSeniorBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignRookieBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignYouthBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignHighAverageAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignHighBlockAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
    }
}