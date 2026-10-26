using System.Data;

using Dapper;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Memberships.Domain;
using Neba.Api.Legacy.Bowlers;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.BowlerMemberships;

internal sealed record LegacyBowlerMembershipRow(int Id, int BowlerId, int MembershipId, DateTime BeginDate, DateTime EndDate, decimal Cost);

internal enum LegacyMembershipOutcome
{
    Created,
    Updated,
    Unchanged,
    Failed
}

internal static class LegacyBowlerMembershipExtensions
{
    extension(BowlerMembership membership)
    {
        public ErrorOr<Updated> ApplyLegacyUpdate(MembershipType type, decimal cost, DateOnly startDate, DateOnly endDate)
        {
            // Same structural checks as BowlerMembership.CreateFromLegacy; no eligibility rules.
            if (cost < 0)
            {
                return BowlerMembershipErrors.NegativeCost;
            }

            if (endDate < startDate)
            {
                return BowlerMembershipErrors.EndDateBeforeStartDate(startDate, endDate);
            }

            membership.Type = type;
            membership.Cost = cost;
            membership.StartDate = startDate;
            membership.EndDate = endDate;

            return Result.Updated;
        }
    }
}

// Shared by BowlerMembershipSyncJob and BowlerMembershipWeeklySyncJob so both decide the same way.
internal static class LegacyBowlerMembershipSync
{
    // The System Import membership: never carried to the website.
    public const int SystemImportMembershipId = 8;

    // Decides create, update or nothing for one Software row against a bowler whose Memberships are loaded.
    public static (LegacyMembershipOutcome Outcome, string? Errors) Apply(Bowler bowler, LegacyBowlerMembershipRow row, MembershipType type)
    {
        ArgumentNullException.ThrowIfNull(bowler);
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(type);

        var startDate = DateOnly.FromDateTime(row.BeginDate);
        var endDate = DateOnly.FromDateTime(row.EndDate);

        var existing = bowler.Memberships.SingleOrDefault(held => held.LegacyId == row.Id);

        if (existing is null)
        {
            var added = bowler.AddLegacyMembership(row.Id, type, row.Cost, startDate, endDate);

            return added.IsError
                ? (LegacyMembershipOutcome.Failed, Describe(added.Errors))
                : (LegacyMembershipOutcome.Created, null);
        }

        // Most rows already match (the per-action call landed). Skipping them avoids a no-op write and audit event.
        if (existing.Type == type && existing.Cost == row.Cost && existing.StartDate == startDate && existing.EndDate == endDate)
        {
            return (LegacyMembershipOutcome.Unchanged, null);
        }

        var updated = existing.ApplyLegacyUpdate(type, row.Cost, startDate, endDate);

        return updated.IsError
            ? (LegacyMembershipOutcome.Failed, Describe(updated.Errors))
            : (LegacyMembershipOutcome.Updated, null);
    }

    private static string Describe(List<Error> errors)
        => string.Join("; ", errors.Select(error => error.Description));
}

internal sealed class BowlerMembershipSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IFusionCache cache,
    ILogger<BowlerMembershipSyncJob> logger)
{
    // Covers the Software adapter's worst case (3 attempts x 5 s timeout + 2 s + 5 s waits, about 20 s)
    // plus queue time, so NewBowlerSyncJob normally creates the bowler first.
    public static readonly TimeSpan BowlerSettleDelay = TimeSpan.FromSeconds(60);

    public async Task UpsertAsync(int legacyId, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var row = await legacyConnection.QuerySingleOrDefaultAsync<LegacyBowlerMembershipRow>(
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
            WHERE
                bm.Id = @Id
            """,
            new
            {
                Id = legacyId
            });
#pragma warning restore DAP005

        if (row is null)
        {
            // Deleted in the Software before this ran (delete beat the delayed add). Nothing to mirror.
            logger.LogLegacyBowlerMembershipNotFound(legacyId);
            return;
        }

        if (row.MembershipId == LegacyBowlerMembershipSync.SystemImportMembershipId)
        {
            logger.LogLegacyBowlerMembershipSkippedSystemImport(legacyId);
            return;
        }

        if (!MembershipType.TryFromLegacyId(row.MembershipId, out var type))
        {
            // Retrying cannot fix an unknown type; log and stop.
            logger.LogLegacyBowlerMembershipTypeUnmapped(legacyId, row.MembershipId);
            return;
        }

        var bowler = await db.Set<Bowler>()
            .Include(b => b.Memberships)
            .SingleOrDefaultAsync(b => b.LegacyId == row.BowlerId, ct);

        var bowlerCreated = false;

        if (bowler is null)
        {
            // Decided: create it from the Software, as UpdateBowlerSyncJob does. NewBowlerSyncJob is a
            // safe no-op when the bowler exists, so a late New Bowler call does no harm. A concurrent
            // create hits the unique LegacyId index; the job throws and Hangfire's retry finds the bowler.
            bowler = await CreateBowlerAsync(row);

            if (bowler is null)
            {
                return;
            }

            bowlerCreated = true;
        }

        var (outcome, errors) = LegacyBowlerMembershipSync.Apply(bowler, row, type);

        if (outcome == LegacyMembershipOutcome.Failed)
        {
            logger.LogLegacyBowlerMembershipApplyFailed(legacyId, errors!);
            return;
        }

        if (bowlerCreated)
        {
            db.Set<Bowler>().Add(bowler);
        }

        if (outcome == LegacyMembershipOutcome.Unchanged)
        {
            return;
        }

        await db.SaveChangesAsync(ct);

        if (bowlerCreated)
        {
            await cache.RemoveByTagAsync("neba:bowlers", token: ct);
        }
    }

    public async Task DeleteAsync(int legacyId, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        var existing = await db.Set<BowlerMembership>().SingleOrDefaultAsync(m => m.LegacyId == legacyId, ct);

        if (existing is null)
        {
            // Decided: idempotent. Already gone (retry, or never synced), the same as the delete
            // convention elsewhere (204 on a missing record).
            logger.LogLegacyBowlerMembershipAlreadyDeleted(legacyId);
            return;
        }

        // Not ExecuteDeleteAsync: it skips Audit.EntityFramework, so no audit event would record the delete.
        db.Set<BowlerMembership>().Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Bowler?> CreateBowlerAsync(LegacyBowlerMembershipRow membership)
    {
#pragma warning disable DAP005
        var row = await legacyConnection.QuerySingleOrDefaultAsync<LegacyBowlerRow>(
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
                Id = @Id
            """,
            new
            {
                Id = membership.BowlerId
            });
#pragma warning restore DAP005

        if (row is null)
        {
            logger.LogLegacyBowlerMissingForMembership(membership.BowlerId, membership.Id);
            return null;
        }

        var created = LegacyBowlerMapping.CreateBowler(row, logger);

        if (created.IsError)
        {
            logger.LogLegacyBowlerMembershipApplyFailed(
                membership.Id,
                string.Join("; ", created.Errors.Select(error => error.Description)));
            return null;
        }

        logger.LogLegacyBowlerCreatedForMembership(membership.BowlerId, membership.Id);

        return created.Value;
    }
}

internal static partial class BowlerMembershipSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No bowler membership found in neba-fwk for legacy id {LegacyBowlerMembershipId}; skipping sync.")]
    public static partial void LogLegacyBowlerMembershipNotFound(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy bowler membership {LegacyBowlerMembershipId} is a System Import record; not carried to the website.")]
    public static partial void LogLegacyBowlerMembershipSkippedSystemImport(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Legacy bowler membership {LegacyBowlerMembershipId} has membership type {LegacyMembershipId}, which maps to no MembershipType; skipping sync.")]
    public static partial void LogLegacyBowlerMembershipTypeUnmapped(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId, int legacyMembershipId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Legacy bowler {LegacyBowlerId} (for membership {LegacyBowlerMembershipId}) is not in neba-fwk; cannot create the bowler or the membership.")]
    public static partial void LogLegacyBowlerMissingForMembership(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerId, int legacyBowlerMembershipId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy bowler {LegacyBowlerId} had no website record when membership {LegacyBowlerMembershipId} synced; created it.")]
    public static partial void LogLegacyBowlerCreatedForMembership(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerId, int legacyBowlerMembershipId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Could not apply legacy bowler membership {LegacyBowlerMembershipId}: {Errors}.")]
    public static partial void LogLegacyBowlerMembershipApplyFailed(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId, string errors);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy bowler membership {LegacyBowlerMembershipId} not on the website; nothing to delete.")]
    public static partial void LogLegacyBowlerMembershipAlreadyDeleted(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId);
}
