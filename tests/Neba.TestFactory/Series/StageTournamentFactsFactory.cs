using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

public static class StageTournamentFactsFactory
{
    public const string ValidTournamentName = "NEBA Singles";
    public static readonly DateOnly ValidStartDate = new(2025, 3, 22);
    public static readonly DateOnly ValidEndDate = new(2025, 3, 22);

    public static StageTournamentFacts Create(
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        SeasonId? seasonId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? otherSeriesName = null)
    {
        var start = startDate ?? ValidStartDate;

        return new()
        {
            TournamentId = tournamentId ?? TournamentId.New(),
            TournamentName = tournamentName ?? ValidTournamentName,
            SeasonId = seasonId ?? SeasonId.New(),
            StartDate = start,
            EndDate = endDate ?? (startDate is null ? ValidEndDate : start),
            OtherSeriesName = otherSeriesName,
        };
    }
}