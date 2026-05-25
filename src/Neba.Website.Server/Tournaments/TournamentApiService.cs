using ErrorOr;

using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Seasons.ListSeasons;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Contracts.Tournaments;
using Neba.Website.Server.History.Champions;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Server.Tournaments;

internal sealed class TournamentApiService(
    ApiExecutor executor,
    ISeasonsApi seasonsApi,
    ITournamentsApi tournamentsApi) : ITournamentApiService
{
    public async Task<ErrorOr<List<SeasonTournamentViewModel>>> GetTournamentsForSeasonAsync(
        SeasonViewModel season, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "SeasonsApi",
            nameof(GetTournamentsForSeasonAsync),
            token => seasonsApi.ListTournamentsInSeasonAsync(season.Id, token),
            ct);

        return result.IsError
            ? result.Errors
            : result.Value.Items.Select(r => MapToViewModel(r, season.Label)).ToList();
    }

    public async Task<ErrorOr<List<SeasonViewModel>>> GetSeasonsAsync(CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "SeasonsApi",
            nameof(GetSeasonsAsync),
            seasonsApi.ListSeasonsAsync,
            ct);

        return result.IsError
            ? result.Errors
            : result.Value.Items.Select(MapToViewModel).ToList();
    }

    public async Task<ErrorOr<List<BowlerTitleSummaryViewModel>>> GetTitleSummariesAsync(CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "TournamentsApi",
            nameof(GetTitleSummariesAsync),
            token => tournamentsApi.ListTournamentChampionsAsync(token),
            ct);

        return result.IsError
            ? result.Errors
            : result.Value.Items.ToTitleSummaries();
    }

    public async Task<ErrorOr<List<TitlesByYearViewModel>>> GetTitlesByYearAsync(CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "TournamentsApi",
            nameof(GetTitlesByYearAsync),
            token => tournamentsApi.ListTournamentChampionsAsync(token),
            ct);

        return result.IsError
            ? result.Errors
            : result.Value.Items.ToTitlesByYear();
    }

    private static SeasonViewModel MapToViewModel(SeasonResponse r) => new()
    {
        Id = r.Id,
        Description = r.Description,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
    };

    private static SeasonTournamentViewModel MapToViewModel(
        SeasonTournamentResponse response, string seasonLabel)
    {
        var firstOilPattern = response.OilPatterns.FirstOrDefault();

        return new SeasonTournamentViewModel
        {
            Id = response.Id,
            Name = response.Name,
            Season = seasonLabel,
            StartDate = response.StartDate,
            EndDate = response.EndDate,
            TournamentType = response.TournamentType,
            EntryFee = response.EntryFee,
            RegistrationUrl = response.RegistrationUrl,
            AddedMoney = response.AddedMoney,
            TournamentLogoUrl = response.LogoUrl,
            BowlingCenterName = response.BowlingCenter?.Name,
            BowlingCenterCity = response.BowlingCenter?.City,
            Sponsor = response.Sponsors.FirstOrDefault()?.Name,
            Winners = response.Winners,
            PatternName = firstOilPattern?.Name,
            PatternLength = firstOilPattern?.Length,
            PatternLengthCategory = response.PatternLengthCategory,
        };
    }
}