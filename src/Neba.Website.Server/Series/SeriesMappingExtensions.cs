using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.Api.Contracts.Series.ListStageResults;
using Neba.Api.Contracts.Series.ListStageSquads;

namespace Neba.Website.Server.Series;

internal static class SeriesMappingExtensions
{
    extension(SeriesSummaryResponse response)
    {
        public SeriesSummaryViewModel ToViewModel() =>
            new()
            {
                Id = response.Id,
                Name = response.Name,
                SeasonYear = response.SeasonStartDate.Year,
                SeasonEndDate = response.SeasonEndDate,
                SeasonDateRange = SeriesDisplay.SeasonRange(response.SeasonStartDate, response.SeasonEndDate),
                QualifyingStageCount = response.QualifyingStageCount,
                SurvivorStageCount = response.SurvivorStageCount,
                PlacementStageCount = response.PlacementStageCount,
                IsLocked = response.IsLocked,
                LogoUrl = response.LogoUrl,
                NextStage = response.NextStage is { } next
                    ? new SeriesNextStageViewModel
                    {
                        Kind = next.Kind,
                        TournamentName = next.TournamentName,
                        Dates = SeriesDisplay.TournamentDates(next.TournamentStartDate, next.TournamentEndDate),
                    }
                    : null,
            };
    }

    extension(SeriesDetailResponse response)
    {
        public SeriesDetailViewModel ToViewModel() =>
            new()
            {
                Id = response.Id,
                Name = response.Name,
                SeasonId = response.SeasonId,
                SeasonDescription = response.SeasonDescription,
                SeasonDateRange = SeriesDisplay.SeasonRange(response.SeasonStartDate, response.SeasonEndDate),
                IsLocked = response.IsLocked,
                Finale = response.Finale is { } finale
                    ? new SeriesFinaleViewModel
                    {
                        FinalistCount = finale.FinalistCount,
                        Date = finale.Date,
                        StartTime = finale.StartTime,
                        BowlingCenterId = finale.BowlingCenterId,
                        Host = SeriesDisplay.Host(finale.BowlingCenterName, finale.BowlingCenterCity, finale.BowlingCenterState),
                    }
                    : null,
                LogoUrl = response.LogoUrl,
                Logo = response.LogoContainer is not null && response.LogoPath is not null
                    ? new SeriesLogoInput
                    {
                        Container = response.LogoContainer,
                        Path = response.LogoPath,
                        ContentType = response.LogoContentType ?? "application/octet-stream",
                        SizeInBytes = response.LogoSizeInBytes ?? 0,
                    }
                    : null,
                Stages = [.. response.Stages.OrderBy(s => s.Order).Select(s => new SeriesStageViewModel
                {
                    Id = s.Id,
                    Order = s.Order,
                    Kind = s.Kind,
                    AdvancingCount = s.AdvancingCount,
                    SquadDate = s.SquadDate,
                    SquadStartTime = s.SquadStartTime,
                    TournamentId = s.TournamentId,
                    TournamentName = s.TournamentName,
                    TournamentStartDate = s.TournamentStartDate,
                    TournamentEndDate = s.TournamentEndDate,
                    TeamSize = s.TeamSize,
                })],
            };
    }

    extension(SeriesAdvancementResponse response)
    {
        /// <summary>Every UTC instant the page shows in local time, once each. The page converts them, then calls ToViewModel.</summary>
        public IReadOnlyList<DateTimeOffset> Instants()
            => [.. response.Stages.Select(s => s.OfficialDueAt).OfType<DateTimeOffset>()
                .Concat(response.Stages.SelectMany(s => s.Overrides).Select(o => o.CreatedAt))
                .Distinct()];

        public SeriesAdvancementViewModel ToViewModel(AdvancementAccess access, IReadOnlyDictionary<DateTimeOffset, DateTimeOffset> localTimes)
        {
            var ordered = response.Stages.OrderBy(s => s.Order).ToList();
            var lastQualifyingId = ordered.LastOrDefault(s => s.Kind == StageKinds.Qualifying)?.StageId;

            return new SeriesAdvancementViewModel
            {
                Version = response.Version,
                BackfillAsOf = response.BackfillAsOf,
                TargetField = response.TargetField,
                Stages = [.. ordered.Select(s => MapStage(s, s.StageId == lastQualifyingId, access, localTimes))],
                Positions = [.. response.Positions.OrderBy(p => p.Position)
                    .Select(p => new PositionViewModel(p.BowlerId, p.BowlerName, p.Position, p.Bowled, p.IsFinalist, p.TournamentPlace))],
            };
        }
    }

    extension(StageResultSummaryResponse response)
    {
        public StageResultViewModel ToViewModel()
            => new(response.BowlerId, response.BowlerName, response.Place, SeriesDisplay.ResultOption(response.BowlerName, response.Place));
    }

    extension(StageSquadSummaryResponse response)
    {
        public StageSquadViewModel ToViewModel()
            => new(response.SquadId, SeriesDisplay.SquadOption(response.Date, response.StartTime),
                SeriesDisplay.SquadDetail(response.HasScores, response.IsSelected), response.HasScores, response.IsSelected);
    }

    private static StageAdvancementViewModel MapStage(
        StageAdvancementResponse stage, bool isLastQualifying, AdvancementAccess access,
        IReadOnlyDictionary<DateTimeOffset, DateTimeOffset> localTimes)
    {
        // Overrides apply to Qualifying Stages while they are Computed, and only Series.ManageAdvancement holders may change them.
        var canChangeOutput = access.CanManage && stage.Kind == StageKinds.Qualifying && stage.Status == StageStatuses.Computed;

        return new StageAdvancementViewModel(
            Id: stage.StageId,
            Order: stage.Order,
            Kind: stage.Kind,
            Status: stage.Status,
            IsStale: stage.IsStale,
            NeedsSquad: stage.NeedsSquad,
            OfficialDueAt: stage.OfficialDueAt is { } due ? ToLocal(due, localTimes) : null,
            SlotCount: stage.SlotCount,
            VacantSlotCount: stage.VacantSlotCount,
            TournamentId: stage.TournamentId,
            TournamentStatus: stage.TournamentStatus,
            ResolvedSquadId: stage.ResolvedSquadId,
            IsLastQualifying: isLastQualifying,
            Entries: [.. stage.Entries.Select(e => new FieldEntryViewModel(
                e.BowlerId, e.BowlerName, e.SourceStageId, e.Place, e.BackfillPoints, e.ViaOverride,
                CanClear: canChangeOutput && e.SourceStageId == stage.StageId && !e.ViaOverride))],
            AlreadyAdvanced: [.. stage.AlreadyAdvanced.Select(a => new AlreadyAdvancedViewModel(
                a.BowlerId, a.BowlerName, a.Place, a.EarlierStageOrder))],
            Overrides: [.. stage.Overrides.Select(o => new StageOverrideViewModel(
                o.OverrideId, o.BowlerId, o.BowlerName, o.Kind, o.Reason, ToLocal(o.CreatedAt, localTimes), o.Applied))],
            Outcomes: [.. stage.SurvivorOutcomes.Select(o => new SurvivorOutcomeViewModel(o.BowlerId, o.BowlerName, o.BlockTotal, o.Advanced))],
            CanAddOverride: canChangeOutput,
            CanRemoveOverride: canChangeOutput,
            CanRecompute: access.CanView,
            CanPickSquad: access.CanView && stage.Kind == StageKinds.Survivor);
    }

    private static DateTimeOffset ToLocal(DateTimeOffset utc, IReadOnlyDictionary<DateTimeOffset, DateTimeOffset> localTimes)
        => localTimes.TryGetValue(utc, out var local) ? local : utc;
}