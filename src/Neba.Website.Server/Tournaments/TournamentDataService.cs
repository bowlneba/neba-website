using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Seasons.ListSeasons;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Server.Tournaments;

internal sealed class TournamentDataService(
    ApiExecutor executor,
    ISeasonsApi seasonsApi) : ITournamentDataService
{
    public async Task<List<SeasonTournamentViewModel>> GetTournamentsForSeasonAsync(
        SeasonViewModel season, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "SeasonsApi",
            nameof(GetTournamentsForSeasonAsync),
            token => seasonsApi.ListTournamentsInSeasonAsync(season.Id, token),
            ct);

        if (result.IsError)
        {
            return [];
        }

        return [.. result.Value.Items.Select(r => MapToViewModel(r, season.Label))];
    }

    public async Task<List<SeasonViewModel>> GetSeasonsAsync(CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            "SeasonsApi",
            nameof(GetSeasonsAsync),
            token => seasonsApi.ListSeasonsAsync(token),
            ct);

        return result.IsError
            ? []
            : [.. result.Value.Items.Select(MapToViewModel)];
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