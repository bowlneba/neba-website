using Neba.Api.Contracts.Tournaments;

namespace Neba.Api.Features.Tournaments;

internal static class TournamentSeriesDtoMapping
{
    extension(TournamentSeriesDto series)
    {
        public TournamentSeriesResponse ToResponse()
            => new()
            {
                SeriesId = series.SeriesId.Value.ToString(),
                Name = series.Name,
                LogoUrl = series.LogoUrl,
                Kind = series.Kind,
                AdvancingCount = series.AdvancingCount,
                SquadDate = series.SquadDate,
                SquadStartTime = series.SquadStartTime,
                FinaleCount = series.FinaleCount,
                FinaleDate = series.FinaleDate,
                FinaleStartTime = series.FinaleStartTime,
            };
    }
}
