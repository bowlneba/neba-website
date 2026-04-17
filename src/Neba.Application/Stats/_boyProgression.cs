#pragma warning disable // This is a temporary file until tournaments come into the application

using System.Diagnostics.CodeAnalysis;

using Neba.Application.Bowlers;
using Neba.Application.Seasons;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;

public sealed record BowlerTournamentPoints(
    int Season,
    int BowlerId,
    int TournamentId,
    string TournamentName,
    DateOnly TournamentDate,
    int CumulativePoints
);

[ExcludeFromCodeCoverage(Justification = "This is a temporary file until tournaments come into the application")]
internal static class _BowlerOfTheYearProgression
{
    public static async Task<IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>> GetBowlerOfTheYearProgressionAsync(SeasonDto season, IBowlerQueries bowlerQueries, CancellationToken cancellationToken)
    {
        var bowlerIdLookup = await bowlerQueries.GetBowlerIdByLegacyIdAsync(cancellationToken);
        var bowlerNames = await bowlerQueries.GetBowlerNamesByIdAsync(cancellationToken);

        var json = await File.ReadAllTextAsync($"/Users/kippermand/Projects/bowlneba/neba-website/src/Neba.Application/Stats/_boy{season.EndDate.Year}.json", cancellationToken);
        var points = System.Text.Json.JsonSerializer.Deserialize<List<BowlerTournamentPoints>>(json);
        var pointsByBowler = points.GroupBy(point => point.BowlerId);

        return pointsByBowler.Select(points => new BowlerOfTheYearPointsRaceSeriesDto
        {
            BowlerId = bowlerIdLookup[points.Key],
            BowlerName = bowlerNames[bowlerIdLookup[points.Key]],
            Results = points.OrderBy(point => point.TournamentDate).Select(point => new BowlerOfTheYearPointsRaceTournamentDto
            {
                TournamentName = point.TournamentName,
                TournamentDate = point.TournamentDate,
                CumulativePoints = point.CumulativePoints
            }).ToList()
        }).ToList();
    }
}