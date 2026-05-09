using ErrorOr;

using Neba.Application.Messaging;
using Neba.Application.Stats.BoyProgression;

namespace Neba.Application.Stats.GetSeasonStats;

internal sealed class GetSeasonStatsQueryHandler(
    ISeasonStatsService seasonStatsService,
    IBowlerOfTheYearProgressionService boyProgressionService)
        : IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>>
{
    private readonly ISeasonStatsService _seasonStatsService = seasonStatsService;
    private readonly IBowlerOfTheYearProgressionService _boyProgressionService = boyProgressionService;

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

        var (numberOfGames, numberOfTournaments, numberOfEntries) = await _seasonStatsService.GetStatMinimumsForSeasonAsync(season, cancellationToken);
        var bowlerStats = await _seasonStatsService.GetBowlerSeasonStatsAsync(season.Id, cancellationToken);
        var bowlerOfTheYearRaces = await _boyProgressionService.GetAllProgressionsAsync(season.Id, cancellationToken);
        var seasonSummary = _seasonStatsService.CalculateSeasonStatsSummary(
            bowlerStats, numberOfGames, numberOfTournaments, numberOfEntries);

        return new SeasonStatsDto
        {
            Season = season,
            SeasonsWithStats = [.. seasonsWithStats.OrderByDescending(s => s.EndDate)],
            BowlerStats = bowlerStats,
            BowlerOfTheYearRaces = bowlerOfTheYearRaces,
            Summary = seasonSummary,
            MinimumNumberOfGames = numberOfGames,
            MinimumNumberOfTournaments = numberOfTournaments,
            MinimumNumberOfEntries = numberOfEntries
        };
    }
}
