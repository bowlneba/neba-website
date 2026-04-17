using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Stats.GetSeasonStats;
using Neba.Application.Messaging;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;

namespace Neba.Api.Stats.GetSeasonStats;

internal sealed class GetSeasonStatsEndpoint(IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>> queryHandler)
    : Endpoint<GetSeasonStatsRequest, GetSeasonStatsResponse>
{
    private readonly IQueryHandler<GetSeasonStatsQuery, ErrorOr<SeasonStatsDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("");
        Group<StatsGroup>();

        Options(options => options
            .WithVersionSet("Stats")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();

        Description(description => description
            .WithName("GetSeasonStats")
            .WithTags("Stats", "Public")
            .Produces<GetSeasonStatsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetSeasonStatsRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new GetSeasonStatsQuery { SeasonYear = req.Year }, ct);

        if (result.IsError)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (result.Value is null)
        {
            AddError("Season stats payload was null.");
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            return;
        }

        // Stryker disable once Statement
        await Send.OkAsync(MapToResponse(result.Value), ct);
    }

    private static GetSeasonStatsResponse MapToResponse(SeasonStatsDto dto) => new()
    {
        SelectedSeason = dto.Season.Description,
        MinimumNumberOfGames = dto.MinimumNumberOfGames,
        MinimumNumberOfTournaments = dto.MinimumNumberOfTournaments,
        MinimumNumberOfEntries = dto.MinimumNumberOfEntries,
        AvailableSeasons = dto.SeasonsWithStats
            .ToDictionary(s => s.EndDate.Year, s => s.Description),
        BowlerSearchList = dto.Summary.BowlerSearchList
            .ToDictionary(e => e.BowlerId.Value.ToString(), e => e.BowlerName.ToDisplayName()),
        BowlerOfTheYear = MapBotyStandings(dto.Summary.BowlerOfTheYear),
        SeniorOfTheYear = MapBotyStandings(dto.Summary.SeniorOfTheYear),
        SuperSeniorOfTheYear = MapBotyStandings(dto.Summary.SuperSeniorOfTheYear),
        WomanOfTheYear = MapBotyStandings(dto.Summary.WomanOfTheYear),
        RookieOfTheYear = MapBotyStandings(dto.Summary.RookieOfTheYear),
        YouthOfTheYear = MapBotyStandings(dto.Summary.YouthOfTheYear),
        HighAverage = [.. dto.Summary.HighAverageLeaderboard
            .Select(h => new HighAverageResponse
            {
                BowlerId = h.BowlerId.Value.ToString(),
                BowlerName = h.BowlerName.ToDisplayName(),
                Average = h.Average,
                Games = h.Games,
                Tournaments = h.Tournaments,
                FieldAverage = h.FieldAverage
            })],
        HighBlock = [.. dto.Summary.HighBlockLeaderboard
            .Select(h => new HighBlockResponse
            {
                BowlerId = h.BowlerId.Value.ToString(),
                BowlerName = h.BowlerName.ToDisplayName(),
                HighBlock = h.HighBlock,
                HighGame = h.HighGame
            })],
        MatchPlayAverage = [.. dto.Summary.MatchPlayAverageLeaderboard
            .Select(m => new MatchPlayAverageResponse
            {
                BowlerId = m.BowlerId.Value.ToString(),
                BowlerName = m.BowlerName.ToDisplayName(),
                MatchPlayAverage = m.MatchPlayAverage,
                Games = m.Games,
                Wins = m.Wins,
                Losses = m.Losses,
                WinPercentage = m.WinPercentage,
                Winnings = m.Winnings
            })],
        MatchPlayRecord = [.. dto.Summary.MatchPlayRecordLeaderboard
            .Select(m => new MatchPlayRecordResponse
            {
                BowlerId = m.BowlerId.Value.ToString(),
                BowlerName = m.BowlerName.ToDisplayName(),
                Wins = m.Wins,
                Losses = m.Losses,
                WinPercentage = m.WinPercentage,
                Finals = m.Finals,
                MatchPlayAverage = m.MatchPlayAverage,
                Winnings = m.Winnings
            })],
        MatchPlayAppearances = [.. dto.Summary.MatchPlayAppearancesLeaderboard
            .Select(m => new MatchPlayAppearancesResponse
            {
                BowlerId = m.BowlerId.Value.ToString(),
                BowlerName = m.BowlerName.ToDisplayName(),
                Finals = m.Finals,
                Tournaments = m.Tournaments,
                Entries = m.Entries
            })],
        PointsPerEntry = [.. dto.Summary.PointsPerEntryLeaderboard
            .Select(p => new PointsPerEntryResponse
            {
                BowlerId = p.BowlerId.Value.ToString(),
                BowlerName = p.BowlerName.ToDisplayName(),
                PointsPerEntry = p.PointsPerEntry,
                Points = p.Points,
                Entries = p.Entries
            })],
        PointsPerTournament = [.. dto.Summary.PointsPerTournamentLeaderboard
            .Select(p => new PointsPerTournamentResponse
            {
                BowlerId = p.BowlerId.Value.ToString(),
                BowlerName = p.BowlerName.ToDisplayName(),
                Points = p.Points,
                Tournaments = p.Tournaments,
                PointsPerTournament = p.PointsPerTournament
            })],
        FinalsPerEntry = [.. dto.Summary.FinalsPerEntryLeaderboard
            .Select(f => new FinalsPerEntryResponse
            {
                BowlerId = f.BowlerId.Value.ToString(),
                BowlerName = f.BowlerName.ToDisplayName(),
                Finals = f.Finals,
                Entries = f.Entries,
                FinalsPerEntry = f.FinalsPerEntry
            })],
        AverageFinishes = [.. dto.Summary.AverageFinishesLeaderboard
            .Select(a => new AverageFinishResponse
            {
                BowlerId = a.BowlerId.Value.ToString(),
                BowlerName = a.BowlerName.ToDisplayName(),
                AverageFinish = a.AverageFinish,
                Finals = a.Finals,
                Winnings = a.Winnings
            })],
        SeasonAtAGlance = new SeasonAtAGlanceResponse
        {
            TotalEntries = dto.Summary.TotalEntries,
            TotalPrizeMoney = dto.Summary.TotalPrizeMoney
        },
        SeasonsBests = new SeasonBestsResponse
        {
            HighGame = dto.Summary.HighGame,
            HighGameBowlers = MapBowlerNames(dto.Summary.HighGameBowlers),
            HighBlock = dto.Summary.HighBlock,
            HighBlockBowlers = MapBowlerNames(dto.Summary.HighBlockBowlers),
            HighAverage = dto.Summary.HighAverage,
            HighAverageBowlers = MapBowlerNames(dto.Summary.HighAverageBowlers)
        },
        FieldMatchPlaySummary = new FieldMatchPlaySummaryResponse
        {
            HighestWinPercentage = dto.Summary.HighestMatchPlayWinPercentage,
            HighestWinPercentageBowlers = MapBowlerNames(dto.Summary.HighestMatchPlayWinPercentageBowlers),
            MostFinals = dto.Summary.MostFinals,
            MostFinalsBowlers = MapBowlerNames(dto.Summary.MostFinalsBowlers)
        },
        BowlerOfTheYearPointsRace = [.. dto.BowlerOfTheYearRace.Select(race => new PointsRaceSeriesResponse
        {
            BowlerId = race.BowlerId.Value.ToString(),
            BowlerName = race.BowlerName.ToDisplayName(),
            Results = [.. race.Results.Select(r => new PointsRaceTournamentResponse
            {
                TournamentName = r.TournamentName,
                TournamentDate = r.TournamentDate,
                CumulativePoints = r.CumulativePoints
            })]
        })],
        AllBowlers = [.. dto.Summary.AllBowlers
            .Select(b => new FullStatModalRowResponse
            {
                BowlerId = b.BowlerId.Value.ToString(),
                BowlerName = b.BowlerName.ToDisplayName(),
                Points = b.Points,
                Average = b.Average,
                Games = b.Games,
                Finals = b.Finals,
                Wins = b.Wins,
                Losses = b.Losses,
                WinPercentage = b.WinPercentage,
                MatchPlayAverage = b.MatchPlayAverage,
                Winnings = b.Winnings,
                FieldAverage = b.FieldAverage,
                Tournaments = b.Tournaments
            })]
    };

    private static IReadOnlyCollection<BowlerOfTheYearStandingResponse> MapBotyStandings(
        IReadOnlyCollection<BowlerOfTheYearStandingDto> standings) =>
        [.. standings.Select(s => new BowlerOfTheYearStandingResponse
        {
            BowlerId = s.BowlerId.Value.ToString(),
            BowlerName = s.BowlerName.ToDisplayName(),
            Points = s.Points,
            Tournaments = s.Tournaments,
            Entries = s.Entries,
            Finals = s.Finals,
            AverageFinish = s.AverageFinish,
            Winnings = s.Winnings
        })];

    private static Dictionary<string, string> MapBowlerNames(IReadOnlyDictionary<BowlerId, Name> bowlers) =>
        bowlers.ToDictionary(kvp => kvp.Key.Value.ToString(), kvp => kvp.Value.ToDisplayName());
}