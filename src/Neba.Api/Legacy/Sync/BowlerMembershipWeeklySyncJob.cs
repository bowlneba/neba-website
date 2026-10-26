using System.Data;
using System.Diagnostics.Metrics;
using System.Globalization;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Memberships.Domain;
using Neba.Api.Legacy.BowlerMemberships;
using Neba.Api.Legacy.Bowlers;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Sync;

internal sealed class BowlerMembershipWeeklySyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IFusionCache cache,
    IManualInterventionNotifier manualInterventionNotifier,
    ILogger<BowlerMembershipWeeklySyncJob> logger)
{
    // After the bowler step, so bowlers exist first and the two jobs do not both create one.
    public static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(15);

    // One save per chunk, not one per run: Audit.EntityFramework writes every changed entry into one
    // audit property, and Azure Table Storage caps a property at 64 KB (see SyncSquadScores.cs).
    private const int SaveBatchSize = 50;

    // SQL Server allows 2100 parameters per command; an IN list takes one each.
    private const int QueryBatchSize = 500;

    // More than this many extras and a human decides: the Software rarely deletes memberships.
    private const int MaxDeletesPerRun = 1;

    private const string MembershipSelect =
        """
        SELECT
            bm.Id,
            bm.BowlerId,
            bm.MembershipId,
            bm.BeginDate,
            bm.EndDate,
            m.Cost
        FROM
            BowlerMemberships bm
        INNER JOIN Memberships m ON m.Id = bm.MembershipId
        """;

    public async Task SyncAsync(DateTime since, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var rows = (await legacyConnection.QueryAsync<LegacyBowlerMembershipRow>(
            $"""
            {MembershipSelect}
            WHERE
                bm.Audit_UpdatedTimestamp >= @Since
                AND bm.MembershipId <> @SystemImport
            ORDER BY
                bm.Id
            """,
            new { Since = since, SystemImport = LegacyBowlerMembershipSync.SystemImportMembershipId })).ToList();
#pragma warning restore DAP005

        var counts = await UpsertAsync(rows, ct);

        logger.LogBowlerMembershipWeeklySyncCompleted(rows.Count, counts.Created, counts.Updated, counts.Unchanged, counts.FailedLegacyIds.Count);

        if (counts.FailedLegacyIds.Count > 0)
        {
            await manualInterventionNotifier.NotifyAsync(
                new DiscordAlert(
                    DiscordAlertSeverity.Warning,
                    "Bowler memberships failed in weekly sync",
                    "These legacy bowler memberships could not be synced to the website (see the job log for each reason). Fix the data in the Software; next week's run picks them up.",
                    new Dictionary<string, string>
                    {
                        ["FailedMembershipCount"] = counts.FailedLegacyIds.Count.ToString(CultureInfo.InvariantCulture),
                        ["LegacyBowlerMembershipIds"] = DiscordFieldFormatter.JoinIds(counts.FailedLegacyIds)
                    }),
                ct);
        }

        await CompareIdsAsync(counts.FailedLegacyIds, ct);
    }

    // A timestamp cannot show a hard delete, so compare the full id sets every run. A count gate would
    // pass when one extra and one missing row offset each other.
    private async Task CompareIdsAsync(List<int> alreadyFailed, CancellationToken ct)
    {
#pragma warning disable DAP005
        var softwareIds = (await legacyConnection.QueryAsync<int>(
            "SELECT Id FROM BowlerMemberships WHERE MembershipId <> @SystemImport",
            new { SystemImport = LegacyBowlerMembershipSync.SystemImportMembershipId })).ToHashSet();
#pragma warning restore DAP005

        // Website-created rows (null LegacyId) are never in this set.
        var websiteIds = (await db.Set<BowlerMembership>()
            .Where(membership => membership.LegacyId != null)
            .Select(membership => membership.LegacyId!.Value)
            .ToListAsync(ct)).ToHashSet();

        var extra = websiteIds.Except(softwareIds).Order().ToList();
        var missing = softwareIds.Except(websiteIds).Order().ToList();

        if (extra.Count == 0 && missing.Count == 0)
        {
            logger.LogBowlerMembershipIdsMatch(softwareIds.Count);
            return;
        }

        logger.LogBowlerMembershipIdsDiffer(softwareIds.Count, websiteIds.Count, extra.Count, missing.Count);

        // A gap means a push call and the changed-since window both missed the row. Healing it is safe
        // even when the delete guard below trips.
        // Rows that just failed in the window step are not retried or alerted a second time.
        var missingCounts = await UpsertMissingAsync(missing.Except(alreadyFailed).ToList(), ct);

        if (softwareIds.Count == 0 && websiteIds.Count > 0)
        {
            await GuardAsync("Software returned no memberships; check the connection and database", extra, websiteIds.Count, ct);
            return;
        }

        var deleted = new List<int>();

        if (extra.Count > MaxDeletesPerRun)
        {
            await GuardAsync($"more than {MaxDeletesPerRun} website memberships are missing from the Software", extra, websiteIds.Count, ct);
        }
        else if (extra.Count > 0)
        {
            deleted = await DeleteExtrasAsync(extra, ct);
        }

        if (deleted.Count > 0)
        {
            logger.LogBowlerMembershipsDeleted(deleted.Count);
            SyncMetrics.MembershipsDeleted.Add(deleted.Count);

            await manualInterventionNotifier.NotifyAsync(
                new DiscordAlert(
                    DiscordAlertSeverity.Warning,
                    "Bowler memberships deleted in weekly sync",
                    "The Software no longer has these memberships, so the weekly sync removed them from the website.",
                    new Dictionary<string, string>
                    {
                        ["DeletedCount"] = deleted.Count.ToString(CultureInfo.InvariantCulture),
                        ["LegacyBowlerMembershipIds"] = DiscordFieldFormatter.JoinIds(deleted),
                        ["UpsertedMissingCount"] = missingCounts.Created.ToString(CultureInfo.InvariantCulture)
                    }),
                ct);
        }
    }

    private async Task GuardAsync(string reason, List<int> extra, int websiteCount, CancellationToken ct)
    {
        logger.LogBowlerMembershipDeleteGuardTripped(reason, extra.Count, websiteCount);
        SyncMetrics.DeleteGuardTripped.Add(1);

        await manualInterventionNotifier.NotifyAsync(
            new DiscordAlert(
                DiscordAlertSeverity.Warning,
                "Bowler membership delete skipped in weekly sync",
                $"Nothing was deleted: {reason}. A person decides whether these website memberships should go.",
                new Dictionary<string, string>
                {
                    ["CandidateCount"] = extra.Count.ToString(CultureInfo.InvariantCulture),
                    ["WebsiteCount"] = websiteCount.ToString(CultureInfo.InvariantCulture),
                    ["LegacyBowlerMembershipIds"] = DiscordFieldFormatter.JoinIds(extra)
                }),
            ct);
    }

    // Re-checks each candidate in the Software right before deleting, which closes the window where a
    // row was added after the id snapshot and synced by a push job.
    private async Task<List<int>> DeleteExtrasAsync(List<int> candidates, CancellationToken ct)
    {
        var deleted = new List<int>();

        foreach (var chunk in candidates.Chunk(QueryBatchSize))
        {
#pragma warning disable DAP005
            var stillPresent = (await legacyConnection.QueryAsync<int>(
                "SELECT Id FROM BowlerMemberships WHERE Id IN @Ids",
                new { Ids = chunk })).ToHashSet();
#pragma warning restore DAP005

            var absent = chunk.Where(id => !stillPresent.Contains(id)).Select(id => (int?)id).ToList();

            foreach (var saveChunk in absent.Chunk(SaveBatchSize))
            {
                var ids = saveChunk.ToList();
                var rows = await db.Set<BowlerMembership>()
                    .Where(membership => ids.Contains(membership.LegacyId))
                    .ToListAsync(ct);

                db.Set<BowlerMembership>().RemoveRange(rows);
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();

                deleted.AddRange(rows.Select(membership => membership.LegacyId!.Value));
            }
        }

        return deleted;
    }

    private async Task<UpsertCounts> UpsertMissingAsync(List<int> missing, CancellationToken ct)
    {
        var rows = new List<LegacyBowlerMembershipRow>(missing.Count);

        foreach (var chunk in missing.Chunk(QueryBatchSize))
        {
#pragma warning disable DAP005
            rows.AddRange(await legacyConnection.QueryAsync<LegacyBowlerMembershipRow>(
                $"""
                {MembershipSelect}
                WHERE
                    bm.Id IN @Ids
                ORDER BY
                    bm.Id
                """,
                new { Ids = chunk }));
#pragma warning restore DAP005
        }

        var counts = await UpsertAsync(rows, ct);

        if (counts.FailedLegacyIds.Count > 0)
        {
            await manualInterventionNotifier.NotifyAsync(
                new DiscordAlert(
                    DiscordAlertSeverity.Warning,
                    "Bowler memberships failed in weekly sync",
                    "These legacy bowler memberships are missing from the website and could not be synced (see the job log for each reason).",
                    new Dictionary<string, string>
                    {
                        ["FailedMembershipCount"] = counts.FailedLegacyIds.Count.ToString(CultureInfo.InvariantCulture),
                        ["LegacyBowlerMembershipIds"] = DiscordFieldFormatter.JoinIds(counts.FailedLegacyIds)
                    }),
                ct);
        }

        return counts;
    }

    private async Task<UpsertCounts> UpsertAsync(List<LegacyBowlerMembershipRow> rows, CancellationToken ct)
    {
        var counts = new UpsertCounts();
        var bowlerCreated = false;

        foreach (var chunk in rows.Chunk(SaveBatchSize))
        {
            foreach (var (row, outcome, created) in await SaveChunkAsync(chunk, ct))
            {
                bowlerCreated |= created;

                switch (outcome)
                {
                    case LegacyMembershipOutcome.Created:
                        counts.Created++;
                        break;
                    case LegacyMembershipOutcome.Updated:
                        counts.Updated++;
                        break;
                    case LegacyMembershipOutcome.Unchanged:
                        counts.Unchanged++;
                        break;
                    default:
                        counts.FailedLegacyIds.Add(row.Id);
                        break;
                }
            }
        }

        if (bowlerCreated)
        {
            await cache.RemoveByTagAsync("neba:bowlers", token: ct);
        }

        return counts;
    }

    // Saves the chunk in one call. If the database rejects it (a unique-key race with a push job), retry
    // row by row so one bad membership goes to the alert and the rest still save.
    private async Task<List<(LegacyBowlerMembershipRow Row, LegacyMembershipOutcome Outcome, bool BowlerCreated)>> SaveChunkAsync(
        LegacyBowlerMembershipRow[] chunk,
        CancellationToken ct)
    {
        try
        {
            var outcomes = await ApplyAsync(chunk, ct);
            await db.SaveChangesAsync(ct);
            return outcomes;
        }
        catch (DbUpdateException exception)
        {
            logger.LogBowlerMembershipWeeklySyncChunkFailed(exception, chunk.Length);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }

        var results = new List<(LegacyBowlerMembershipRow, LegacyMembershipOutcome, bool)>(chunk.Length);

        foreach (var row in chunk)
        {
            try
            {
                var outcome = (await ApplyAsync([row], ct)).Single();
                await db.SaveChangesAsync(ct);
                results.Add(outcome);
            }
            catch (DbUpdateException exception)
            {
                logger.LogBowlerMembershipWeeklySyncRowSaveFailed(exception, row.Id);
                results.Add((row, LegacyMembershipOutcome.Failed, false));
            }
            finally
            {
                db.ChangeTracker.Clear();
            }
        }

        return results;
    }

    private async Task<List<(LegacyBowlerMembershipRow Row, LegacyMembershipOutcome Outcome, bool BowlerCreated)>> ApplyAsync(
        LegacyBowlerMembershipRow[] chunk,
        CancellationToken ct)
    {
        var legacyBowlerIds = chunk.Select(row => (int?)row.BowlerId).Distinct().ToList();

        var bowlers = await db.Set<Bowler>()
            .Include(bowler => bowler.Memberships)
            .Where(bowler => legacyBowlerIds.Contains(bowler.LegacyId))
            .ToDictionaryAsync(bowler => bowler.LegacyId!.Value, ct);

        var missingBowlerIds = chunk.Select(row => row.BowlerId).Distinct().Where(id => !bowlers.ContainsKey(id)).ToList();
        var createdBowlerIds = new HashSet<int>();

        if (missingBowlerIds.Count > 0)
        {
#pragma warning disable DAP005
            var bowlerRows = await legacyConnection.QueryAsync<LegacyBowlerRow>(
                """
                SELECT
                    Id,
                    FirstName,
                    MiddleInitial,
                    LastName,
                    Suffix,
                    Gender,
                    DateOfBirth
                FROM
                    Bowlers
                WHERE
                    Id IN @Ids
                """,
                new { Ids = missingBowlerIds });
#pragma warning restore DAP005

            foreach (var bowlerRow in bowlerRows)
            {
                var created = LegacyBowlerMapping.CreateBowler(bowlerRow, logger);

                if (created.IsError)
                {
                    logger.LogLegacyBowlerCreateForMembershipWeeklySyncFailed(
                        bowlerRow.Id,
                        string.Join("; ", created.Errors.Select(error => error.Description)));
                    continue;
                }

                db.Set<Bowler>().Add(created.Value);
                bowlers[bowlerRow.Id] = created.Value;
                createdBowlerIds.Add(bowlerRow.Id);
            }
        }

        var results = new List<(LegacyBowlerMembershipRow, LegacyMembershipOutcome, bool)>(chunk.Length);

        foreach (var row in chunk)
        {
            if (!bowlers.TryGetValue(row.BowlerId, out var bowler))
            {
                logger.LogLegacyBowlerMissingForMembershipWeeklySync(row.BowlerId, row.Id);
                results.Add((row, LegacyMembershipOutcome.Failed, false));
                continue;
            }

            if (!MembershipType.TryFromLegacyId(row.MembershipId, out var type))
            {
                logger.LogLegacyBowlerMembershipWeeklySyncTypeUnmapped(row.Id, row.MembershipId);
                results.Add((row, LegacyMembershipOutcome.Failed, false));
                continue;
            }

            var (outcome, errors) = LegacyBowlerMembershipSync.Apply(bowler, row, type);

            if (outcome == LegacyMembershipOutcome.Failed)
            {
                logger.LogLegacyBowlerMembershipWeeklySyncFailed(row.Id, errors!);
            }

            results.Add((row, outcome, createdBowlerIds.Contains(row.BowlerId)));
        }

        return results;
    }

    internal static class SyncMetrics
    {
        private static readonly Meter Meter = new("Neba.Api.Legacy.Sync");

        public static readonly Counter<long> MembershipsDeleted = Meter.CreateCounter<long>(
            "neba.api.legacy.membership_sync.deleted",
            description: "Bowler memberships the weekly sync deleted because the Software no longer has them");

        public static readonly Counter<long> DeleteGuardTripped = Meter.CreateCounter<long>(
            "neba.api.legacy.membership_sync.delete_guard_tripped",
            description: "Weekly sync runs where the delete guard skipped deletes for a person to decide");
    }

    private sealed class UpsertCounts
    {
        public int Created { get; set; }

        public int Updated { get; set; }

        public int Unchanged { get; set; }

        public List<int> FailedLegacyIds { get; } = [];
    }
}

internal static partial class BowlerMembershipWeeklySyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Weekly bowler membership sync finished: {Total} changed in the Software, {Created} created, {Updated} updated, {Unchanged} already current, {Failed} failed.")]
    public static partial void LogBowlerMembershipWeeklySyncCompleted(this ILogger<BowlerMembershipWeeklySyncJob> logger, int total, int created, int updated, int unchanged, int failed);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Bowler membership ids match: {Count} in the Software and on the website.")]
    public static partial void LogBowlerMembershipIdsMatch(this ILogger<BowlerMembershipWeeklySyncJob> logger, int count);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Bowler membership ids differ: {SoftwareCount} in the Software, {WebsiteCount} on the website; {Extra} extra and {Missing} missing.")]
    public static partial void LogBowlerMembershipIdsDiffer(this ILogger<BowlerMembershipWeeklySyncJob> logger, int softwareCount, int websiteCount, int extra, int missing);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Deleted {Deleted} website bowler memberships the Software no longer has.")]
    public static partial void LogBowlerMembershipsDeleted(this ILogger<BowlerMembershipWeeklySyncJob> logger, int deleted);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Skipped deleting website bowler memberships: {Reason} ({Extra} candidates, {WebsiteCount} on the website).")]
    public static partial void LogBowlerMembershipDeleteGuardTripped(this ILogger<BowlerMembershipWeeklySyncJob> logger, string reason, int extra, int websiteCount);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Weekly sync could not save a chunk of {Count} bowler memberships; retrying each on its own.")]
    public static partial void LogBowlerMembershipWeeklySyncChunkFailed(this ILogger<BowlerMembershipWeeklySyncJob> logger, Exception exception, int count);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Weekly sync could not save legacy bowler membership {LegacyBowlerMembershipId}.")]
    public static partial void LogBowlerMembershipWeeklySyncRowSaveFailed(this ILogger<BowlerMembershipWeeklySyncJob> logger, Exception exception, int legacyBowlerMembershipId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Weekly sync could not apply legacy bowler membership {LegacyBowlerMembershipId}: {Errors}.")]
    public static partial void LogLegacyBowlerMembershipWeeklySyncFailed(this ILogger<BowlerMembershipWeeklySyncJob> logger, int legacyBowlerMembershipId, string errors);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Weekly sync: legacy bowler membership {LegacyBowlerMembershipId} has membership type {LegacyMembershipId}, which maps to no MembershipType.")]
    public static partial void LogLegacyBowlerMembershipWeeklySyncTypeUnmapped(this ILogger<BowlerMembershipWeeklySyncJob> logger, int legacyBowlerMembershipId, int legacyMembershipId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Weekly sync: legacy bowler {LegacyBowlerId} (for membership {LegacyBowlerMembershipId}) is not in neba-fwk; cannot create the bowler or the membership.")]
    public static partial void LogLegacyBowlerMissingForMembershipWeeklySync(this ILogger<BowlerMembershipWeeklySyncJob> logger, int legacyBowlerId, int legacyBowlerMembershipId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Weekly sync could not apply legacy bowler {LegacyBowlerId} while creating it for a membership: {Errors}.")]
    public static partial void LogLegacyBowlerCreateForMembershipWeeklySyncFailed(this ILogger<BowlerMembershipWeeklySyncJob> logger, int legacyBowlerId, string errors);
}
