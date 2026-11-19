using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeries;

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
}