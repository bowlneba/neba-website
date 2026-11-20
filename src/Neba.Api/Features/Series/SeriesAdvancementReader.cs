using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal sealed record AdvancementHeaderRow(int Version, DateOnly? BackfillAsOf);

internal sealed record AdvancementStageRow(
    SeriesStageId StageId,
    int Order,
    StageKind Kind,
    StageStatus Status,
    int? AdvancingCount,
    DateOnly? SquadDate,
    TimeOnly? SquadStartTime,
    SquadId? SquadId,
    bool SquadExists,
    bool IsStale,
    DateTimeOffset? ComputedAt,
    DateTimeOffset? OfficialAt,
    int? SlotCount,
    TournamentId TournamentId,
    string TournamentName,
    TournamentStatus TournamentStatus,
    TournamentType TournamentType);

internal sealed record AdvancementFieldEntryRow(
    SeriesFieldEntryId EntryId, BowlerId BowlerId, Name BowlerName,
    SeriesStageId? SourceStageId, int? Place, int? BackfillPoints, bool ViaOverride);

internal sealed record AdvancementSurvivorOutcomeRow(
    SeriesStageId StageId, BowlerId BowlerId, Name BowlerName, int? BlockTotal, bool Advanced);

internal sealed record AdvancementPositionRow(
    BowlerId BowlerId, Name BowlerName, int Position, bool Bowled, bool? IsFinalist);

internal sealed record AdvancementOverrideRow(
    StageOverrideId OverrideId, SeriesStageId StageId, BowlerId BowlerId, Name BowlerName,
    OverrideKind Kind, string Reason, string CreatedBy, DateTimeOffset CreatedAt, bool Applied);

internal sealed record SeriesAdvancementRows(
    AdvancementHeaderRow Header,
    IReadOnlyList<AdvancementStageRow> Stages,
    IReadOnlyList<AdvancementFieldEntryRow> FieldEntries,
    IReadOnlyList<AdvancementSurvivorOutcomeRow> SurvivorOutcomes,
    IReadOnlyList<AdvancementPositionRow> Positions,
    IReadOnlyList<AdvancementOverrideRow> Overrides);

internal static class SeriesAdvancementReader
{
    private const int MaxAttempts = 3;

    /// <summary>Null when the Series does not exist. Retries when a write lands between the queries.</summary>
    public static async Task<SeriesAdvancementRows?> ReadAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
    {
        SeriesAdvancementRows? rows = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var header = await ReadHeaderAsync(appDbContext, seriesId, cancellationToken);
            if (header is null)
            {
                return null;
            }

            rows = new SeriesAdvancementRows(
                header,
                await ReadStagesAsync(appDbContext, seriesId, cancellationToken),
                await ReadFieldEntriesAsync(appDbContext, seriesId, cancellationToken),
                await ReadSurvivorOutcomesAsync(appDbContext, seriesId, cancellationToken),
                await ReadPositionsAsync(appDbContext, seriesId, cancellationToken),
                await ReadOverridesAsync(appDbContext, seriesId, cancellationToken));

            // Every output or status change bumps Version, so an unchanged Version means no write landed in between.
            var after = await ReadHeaderAsync(appDbContext, seriesId, cancellationToken);
            if (after?.Version == header.Version)
            {
                break;
            }
        }

        // After the last attempt the rows keep the first Version read: a write the page then sends gets a 409, which is safe.
        return rows;
    }

    private static Task<AdvancementHeaderRow?> ReadHeaderAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .Select(series => new AdvancementHeaderRow(series.Version, series.BackfillAsOf))
            .SingleOrDefaultAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementStageRow>> ReadStagesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .Join(appDbContext.Tournaments.AsNoTracking(), stage => stage.TournamentId, tournament => tournament.Id,
                (stage, tournament) => new { stage, tournament })
            .OrderBy(x => x.stage.Order)
            .Select(x => new AdvancementStageRow(
                x.stage.Id,
                x.stage.Order,
                x.stage.Kind,
                x.stage.Status,
                x.stage.AdvancingCount,
                x.stage.SquadDesignation == null ? (DateOnly?)null : x.stage.SquadDesignation.Date,
                x.stage.SquadDesignation == null ? (TimeOnly?)null : x.stage.SquadDesignation.StartTime,
                x.stage.SquadDesignation == null ? null : x.stage.SquadDesignation.SquadId,
                // No FK on squad_id, so a vanished squad shows here as false (feeds "Needs squad").
                x.stage.SquadDesignation != null
                    && x.tournament.Squads.Any(squad => squad.Id == x.stage.SquadDesignation.SquadId),
                x.stage.IsStale,
                x.stage.ComputedAt,
                x.stage.OfficialAt,
                x.stage.SlotCount,
                x.tournament.Id,
                x.tournament.Name,
                x.tournament.Status,
                x.tournament.TournamentType))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementFieldEntryRow>> ReadFieldEntriesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.FieldEntries)
            .Select(entry => new AdvancementFieldEntryRow(
                entry.Id, entry.BowlerId, entry.Bowler.Name,
                entry.SourceStageId, entry.Place, entry.BackfillPoints, entry.ViaOverride))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementSurvivorOutcomeRow>> ReadSurvivorOutcomesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .SelectMany(stage => stage.SurvivorOutcomes, (stage, outcome) => new AdvancementSurvivorOutcomeRow(
                stage.Id, outcome.BowlerId, outcome.Bowler.Name, outcome.BlockTotal, outcome.Advanced))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementPositionRow>> ReadPositionsAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Positions)
            .OrderBy(position => position.Position)
            .Select(position => new AdvancementPositionRow(
                position.BowlerId, position.Bowler.Name, position.Position, position.Bowled, position.IsFinalist))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementOverrideRow>> ReadOverridesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .SelectMany(stage => stage.Overrides, (stage, stageOverride) => new AdvancementOverrideRow(
                stageOverride.Id, stage.Id, stageOverride.BowlerId, stageOverride.Bowler.Name,
                stageOverride.Kind, stageOverride.Reason, stageOverride.CreatedBy, stageOverride.CreatedAt,
                stageOverride.Applied))
            .ToListAsync(cancellationToken);
}