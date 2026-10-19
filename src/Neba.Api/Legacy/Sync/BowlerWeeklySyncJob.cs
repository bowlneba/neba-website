using System.Data;
using System.Globalization;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Legacy.Bowlers;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Sync;

internal sealed class BowlerWeeklySyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IFusionCache cache,
    IManualInterventionNotifier manualInterventionNotifier,
    ILogger<BowlerWeeklySyncJob> logger)
{
    // One save per chunk, not one per run: Audit.EntityFramework writes every changed entry into one
    // audit property, and Azure Table Storage caps a property at 64 KB (see SyncSquadScores.cs).
    private const int SaveBatchSize = 50;

    public async Task SyncAsync(DateTime since, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var rows = (await legacyConnection.QueryAsync<LegacyBowlerRow>(
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
                Audit_UpdatedTimestamp >= @Since
            ORDER BY
                Id
            """,
            new { Since = since })).ToList();
#pragma warning restore DAP005

        var created = 0;
        var updated = 0;
        var unchanged = 0;
        var failedLegacyIds = new List<int>();

        foreach (var chunk in rows.Chunk(SaveBatchSize))
        {
            var outcomes = await SaveChunkAsync(chunk, ct);

            foreach (var (row, outcome) in outcomes)
            {
                switch (outcome)
                {
                    case Outcome.Created:
                        created++;
                        break;
                    case Outcome.Updated:
                        updated++;
                        break;
                    case Outcome.Unchanged:
                        unchanged++;
                        break;
                    default:
                        failedLegacyIds.Add(row.Id);
                        break;
                }
            }
        }

        logger.LogBowlerWeeklySyncCompleted(rows.Count, created, updated, unchanged, failedLegacyIds.Count);

        if (created + updated > 0)
        {
            await cache.RemoveByTagAsync("neba:bowlers", token: ct);
        }

        if (failedLegacyIds.Count > 0)
        {
            await manualInterventionNotifier.NotifyAsync(
                new DiscordAlert(
                    DiscordAlertSeverity.Warning,
                    "Bowlers failed in weekly sync",
                    "These legacy bowlers could not be synced to the website (see the job log for each reason). Fix the data in the Software; next week's run picks them up.",
                    new Dictionary<string, string>
                    {
                        ["FailedBowlerCount"] = failedLegacyIds.Count.ToString(CultureInfo.InvariantCulture),
                        ["LegacyBowlerIds"] = DiscordFieldFormatter.JoinLines(
                            failedLegacyIds.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList())
                    }),
                ct);
        }
    }

    // Saves the chunk in one call. If the database rejects it (a unique-key race with NewBowlerSyncJob,
    // a concurrency conflict), retry row by row so one bad bowler goes to the alert and the rest still
    // save, instead of the whole run failing and retrying.
    private async Task<List<(LegacyBowlerRow Row, Outcome Outcome)>> SaveChunkAsync(LegacyBowlerRow[] chunk, CancellationToken ct)
    {
        try
        {
            var outcomes = await ApplyAsync(chunk, ct);
            await db.SaveChangesAsync(ct);
            return outcomes;
        }
        catch (DbUpdateException exception)
        {
            logger.LogBowlerWeeklySyncChunkFailed(exception, chunk.Length);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }

        var results = new List<(LegacyBowlerRow, Outcome)>(chunk.Length);

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
                logger.LogBowlerWeeklySyncRowSaveFailed(exception, row.Id);
                results.Add((row, Outcome.Failed));
            }
            finally
            {
                db.ChangeTracker.Clear();
            }
        }

        return results;
    }

    private async Task<List<(LegacyBowlerRow Row, Outcome Outcome)>> ApplyAsync(LegacyBowlerRow[] chunk, CancellationToken ct)
    {
        var legacyIds = chunk.Select(row => (int?)row.Id).ToList();
        var existingByLegacyId = await db.Set<Bowler>()
            .Where(bowler => legacyIds.Contains(bowler.LegacyId))
            .ToDictionaryAsync(bowler => bowler.LegacyId!.Value, ct);

        return chunk
            .Select(row => (row, Apply(row, existingByLegacyId.GetValueOrDefault(row.Id))))
            .ToList();
    }

    private Outcome Apply(LegacyBowlerRow row, Bowler? existing)
    {
        var gender = LegacyBowlerMapping.MapGender(row.Gender);
        var dateOfBirth = LegacyBowlerMapping.MapDateOfBirth(row.DateOfBirth);
        var suffix = LegacyBowlerMapping.MapSuffix(row.Suffix);
        if (suffix is null && !string.IsNullOrWhiteSpace(row.Suffix))
        {
            logger.LogLegacySuffixUnmappedForWeeklySync(row.Id, row.Suffix);
        }

        var (firstName, nickname) = row.FirstName.ExtractQuotedNickname();

        if (existing is null)
        {
            var bowler = Bowler.CreateFromLegacy(
                firstName,
                row.LastName,
                middleName: row.MiddleInitial,
                suffix: suffix,
                nickname: nickname,
                legacyId: row.Id,
                gender: gender,
                dateOfBirth: dateOfBirth);

            if (bowler.IsError)
            {
                logger.LogLegacyBowlerWeeklySyncFailed(row.Id, string.Join("; ", bowler.Errors.Select(error => error.Description)));
                return Outcome.Failed;
            }

            db.Set<Bowler>().Add(bowler.Value);
            return Outcome.Created;
        }

        // The Software has no nickname field, so a name without quotes says nothing about the nickname.
        // Keep the one on the website; only a quoted nickname in the legacy first name replaces it.
        nickname ??= existing.Name.Nickname;

        var name = Name.Create(firstName, row.LastName, row.MiddleInitial, suffix, nickname);
        if (name.IsError)
        {
            logger.LogLegacyBowlerWeeklySyncFailed(row.Id, string.Join("; ", name.Errors.Select(error => error.Description)));
            return Outcome.Failed;
        }

        // Most of the window already matches (the per-action call usually landed). Skipping those
        // avoids a no-op write and audit event per row.
        if (existing.Name == name.Value && existing.Gender == gender && existing.DateOfBirth == dateOfBirth)
        {
            return Outcome.Unchanged;
        }

        var result = existing.ApplyLegacyUpdate(
            firstName,
            row.LastName,
            middleName: row.MiddleInitial,
            suffix: suffix,
            nickname: nickname,
            gender: gender,
            dateOfBirth: dateOfBirth);

        if (result.IsError)
        {
            logger.LogLegacyBowlerWeeklySyncFailed(row.Id, string.Join("; ", result.Errors.Select(error => error.Description)));
            return Outcome.Failed;
        }

        // Same owned-Name audit workaround as UpdateBowlerSyncJob: force Bowler's own entry to
        // Modified so the audit event carries the bowler's ids, not just the owned Name entry.
        db.Entry(existing).State = EntityState.Modified;
        return Outcome.Updated;
    }

    private enum Outcome
    {
        Created,
        Updated,
        Unchanged,
        Failed
    }
}

internal static partial class BowlerWeeklySyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Weekly bowler sync finished: {Total} changed in the Software, {Created} created, {Updated} updated, {Unchanged} already current, {Failed} failed.")]
    public static partial void LogBowlerWeeklySyncCompleted(this ILogger<BowlerWeeklySyncJob> logger, int total, int created, int updated, int unchanged, int failed);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Weekly sync could not apply legacy bowler {LegacyBowlerId}: {Errors}.")]
    public static partial void LogLegacyBowlerWeeklySyncFailed(this ILogger<BowlerWeeklySyncJob> logger, int legacyBowlerId, string errors);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not map legacy suffix '{LegacySuffix}' (bowler {LegacyBowlerId}) to a known NameSuffix; leaving suffix blank.")]
    public static partial void LogLegacySuffixUnmappedForWeeklySync(this ILogger<BowlerWeeklySyncJob> logger, int legacyBowlerId, string legacySuffix);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Weekly sync could not save a chunk of {Count} bowlers; retrying each bowler on its own.")]
    public static partial void LogBowlerWeeklySyncChunkFailed(this ILogger<BowlerWeeklySyncJob> logger, Exception exception, int count);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Weekly sync could not save legacy bowler {LegacyBowlerId}.")]
    public static partial void LogBowlerWeeklySyncRowSaveFailed(this ILogger<BowlerWeeklySyncJob> logger, Exception exception, int legacyBowlerId);
}
