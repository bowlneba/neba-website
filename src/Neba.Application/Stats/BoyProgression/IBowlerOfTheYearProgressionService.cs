using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;

namespace Neba.Application.Stats.BoyProgression;

internal interface IBowlerOfTheYearProgressionService
{
    Task<IReadOnlyDictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>>
        GetAllProgressionsAsync(SeasonId seasonId, CancellationToken cancellationToken);
}