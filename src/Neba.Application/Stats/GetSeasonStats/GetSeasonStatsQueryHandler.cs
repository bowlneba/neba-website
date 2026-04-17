using ErrorOr;

using Neba.Application.Bowlers;
using Neba.Application.Messaging;

namespace Neba.Application.Stats.GetSeasonStats;

internal sealed class GetSeasonStatsQueryHandler(ISeasonStatsService seasonStatsService, IBowlerQueries bowlerQueries)
        : IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>>
{
    private readonly ISeasonStatsService _seasonStatsService = seasonStatsService;

    /// <summary>
    /// This will be removed when tournaments and result stats come into the software, until then it is needed to show the progression of the bowler of the
    /// </summary>
    private readonly IBowlerQueries _bowlerQueries = bowlerQueries;

    public async Task<ErrorOr<SeasonStatsDto>> HandleAsync(GetSeasonStatsQuery query, CancellationToken cancellationToken)
    {
        var seasonsWithStats = await _seasonStatsService.GetSeasonsWithStatsAsync(cancellationToken);

        var season = query.SeasonYear.HasValue
            ? seasonsWithStats.FirstOrDefault(season => season.EndDate.Year == query.SeasonYear.Value || season.StartDate.Year == query.SeasonYear.Value)
            : seasonsWithStats.OrderByDescending(season => season.EndDate).FirstOrDefault();

        if (season is null)
        {
            return StatsErrors.SeasonHasNoStats;
        }

        var bowlerStats = await _seasonStatsService.GetBowlerSeasonStatsAsync(season.Id, cancellationToken);
        var bowlerOfTheYearRace = await _seasonStatsService.GetBowlerOfTheYearRaceAsync(season, _bowlerQueries, cancellationToken);
        var seasonSummary = _seasonStatsService.CalculateSeasonStatsSummary(bowlerStats);

        return new SeasonStatsDto
        {
            Season = season,
            SeasonsWithStats = [.. seasonsWithStats.OrderByDescending(s => s.EndDate)],
            BowlerStats = bowlerStats,
            BowlerOfTheYearRace = bowlerOfTheYearRace,
            Summary = seasonSummary
        };
    }
}