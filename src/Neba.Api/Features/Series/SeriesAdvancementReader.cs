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

/// <summary>A result row inside a Qualifying Stage's cut whose bowler already advanced from an earlier Stage.</summary>
internal sealed record AdvancementAlreadyAdvancedRow(
    SeriesStageId StageId, BowlerId BowlerId, Name BowlerName, int Place, SeriesStageId EarlierStageId);

internal sealed record AdvancementSurvivorOutcomeRow(
    SeriesStageId StageId, BowlerId BowlerId, Name BowlerName, int? BlockTotal, bool Advanced);

internal sealed record AdvancementPositionRow(
    BowlerId BowlerId, Name BowlerName, int Position, bool Bowled, bool? IsFinalist, int? TournamentPlace = null);

internal sealed record AdvancementOverrideRow(
    StageOverrideId OverrideId, SeriesStageId StageId, BowlerId BowlerId, Name BowlerName,
    OverrideKind Kind, string Reason, string CreatedBy, DateTimeOffset CreatedAt, bool Applied);

internal sealed record SeriesAdvancementRows(
    AdvancementHeaderRow Header,
    IReadOnlyList<AdvancementStageRow> Stages,
    IReadOnlyList<AdvancementFieldEntryRow> FieldEntries,
    IReadOnlyList<AdvancementAlreadyAdvancedRow> AlreadyAdvanced,
    IReadOnlyList<AdvancementSurvivorOutcomeRow> SurvivorOutcomes,
    IReadOnlyList<AdvancementPositionRow> Positions,
    IReadOnlyList<AdvancementOverrideRow> Overrides);

internal static class SeriesAdvancementReader
{
    private const int MaxAttempts = 3;

    /// <summary>Null when the Series does not exist. Retries when a write lands between the queries.</summary>
    public static Task<SeriesAdvancementRows?> ReadAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => ReadCoreAsync(appDbContext, seriesId, ReadOverridesAsync, MaxAttempts, cancellationToken);

    /// <summary>
    /// The same read for the public page. Skips the admin-only overrides (reason and author), which the public view never shows.
    /// It reads once, with no version check or retry, because the public view sends no write.
    /// </summary>
    public static Task<SeriesAdvancementRows?> ReadPublicAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => ReadCoreAsync(appDbContext, seriesId, NoOverridesAsync, 1, cancellationToken);

    private static async Task<SeriesAdvancementRows?> ReadCoreAsync(
        AppDbContext appDbContext,
        SeriesId seriesId,
        Func<AppDbContext, SeriesId, CancellationToken, Task<IReadOnlyList<AdvancementOverrideRow>>> readOverrides,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        SeriesAdvancementRows? rows = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var header = await ReadHeaderAsync(appDbContext, seriesId, cancellationToken);
            if (header is null)
            {
                return null;
            }

            var stages = await ReadStagesAsync(appDbContext, seriesId, cancellationToken);
            var fieldEntries = await ReadFieldEntriesAsync(appDbContext, seriesId, cancellationToken);

            rows = new SeriesAdvancementRows(
                header,
                stages,
                fieldEntries,
                await ReadAlreadyAdvancedAsync(appDbContext, stages, fieldEntries, cancellationToken),
                await ReadSurvivorOutcomesAsync(appDbContext, seriesId, cancellationToken),
                await ReadPositionsAsync(appDbContext, seriesId, stages, cancellationToken),
                await readOverrides(appDbContext, seriesId, cancellationToken));

            if (maxAttempts == 1)
            {
                break;
            }

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

    // A slot row whose bowler already advanced from an earlier Qualifying Stage stays vacant (QualifyingComputation). The
    // stored output has no row for it, so find it again here from the results: place within the cut, bowler already in the
    // field from an earlier Stage.
    private static async Task<IReadOnlyList<AdvancementAlreadyAdvancedRow>> ReadAlreadyAdvancedAsync(
        AppDbContext appDbContext,
        IReadOnlyList<AdvancementStageRow> stages,
        IReadOnlyList<AdvancementFieldEntryRow> fieldEntries,
        CancellationToken cancellationToken)
    {
        var stageOrder = stages.ToDictionary(stage => stage.StageId, stage => stage.Order);

        // The earlier Stage each bowler advanced from (Stage entries only, not the Backfill).
        var advancedFrom = fieldEntries
            .Where(entry => entry.SourceStageId is { } id && stageOrder.ContainsKey(id))
            .ToDictionary(entry => entry.BowlerId, entry => entry.SourceStageId!.Value);

        var qualifying = stages
            .Where(stage => stage.Kind == StageKind.Qualifying && stage.SlotCount is not null && stage.AdvancingCount is > 0)
            .ToList();

        if (qualifying.Count == 0 || advancedFrom.Count == 0)
        {
            return [];
        }

        var tournamentIds = qualifying.Select(stage => stage.TournamentId).Distinct().ToList();
        var bowlerIds = advancedFrom.Keys.ToList();
        var maxPlace = qualifying.Max(stage => stage.AdvancingCount!.Value);

        var results = await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .SelectMany(
                tournament => tournament.Results.Where(result => result.Place > 0 && result.Place <= maxPlace && bowlerIds.Contains(result.BowlerId)),
                (tournament, result) => new { TournamentId = tournament.Id, result.BowlerId, result.Bowler.Name, result.Place })
            .ToListAsync(cancellationToken);

        return [.. qualifying
            .SelectMany(stage => results
                .Where(result => result.TournamentId == stage.TournamentId && result.Place <= stage.AdvancingCount!.Value)
                .Where(result => stageOrder[advancedFrom[result.BowlerId]] < stage.Order)
                .Select(result => new AdvancementAlreadyAdvancedRow(
                    stage.StageId, result.BowlerId, result.Name, result.Place, advancedFrom[result.BowlerId])))
            .OrderBy(row => row.Place)
            .ThenBy(row => row.BowlerId.Value)];
    }

    private static async Task<IReadOnlyList<AdvancementSurvivorOutcomeRow>> ReadSurvivorOutcomesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .SelectMany(stage => stage.SurvivorOutcomes, (stage, outcome) => new AdvancementSurvivorOutcomeRow(
                stage.Id, outcome.BowlerId, outcome.Bowler.Name, outcome.BlockTotal, outcome.Advanced))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementPositionRow>> ReadPositionsAsync(
        AppDbContext appDbContext, SeriesId seriesId, IReadOnlyList<AdvancementStageRow> stages, CancellationToken cancellationToken)
    {
        var positions = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Positions)
            .OrderBy(position => position.Position)
            .Select(position => new AdvancementPositionRow(
                position.BowlerId, position.Bowler.Name, position.Position, position.Bowled, position.IsFinalist))
            .ToListAsync(cancellationToken);

        var placementTournamentId = stages.FirstOrDefault(stage => stage.Kind == StageKind.Placement)?.TournamentId;
        if (positions.Count == 0 || placementTournamentId is null)
        {
            return positions;
        }

        // Where each bowler finished in the Placement Stage's tournament (place 0 means unplaced).
        var bowlerIds = positions.Select(position => position.BowlerId).ToList();
        var places = await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.Id == placementTournamentId)
            .SelectMany(tournament => tournament.Results.Where(result => result.Place > 0 && bowlerIds.Contains(result.BowlerId)))
            .Select(result => new { result.BowlerId, result.Place })
            .ToListAsync(cancellationToken);
        var placeByBowler = places.GroupBy(result => result.BowlerId).ToDictionary(group => group.Key, group => group.Min(result => result.Place));

        return [.. positions.Select(position => placeByBowler.TryGetValue(position.BowlerId, out var place)
            ? position with { TournamentPlace = place }
            : position)];
    }

    private static Task<IReadOnlyList<AdvancementOverrideRow>> NoOverridesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<AdvancementOverrideRow>>([]);

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