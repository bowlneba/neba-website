using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.FileProviders;

using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Seasons.ListSeasons;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Server.Tournaments;

internal sealed class TournamentDataService(
    IWebHostEnvironment env,
    ApiExecutor executor,
    ISeasonsApi seasonsApi) : ITournamentDataService
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<List<TournamentSummaryViewModel>> GetTournamentsForSeasonAsync(
        SeasonViewModel season, CancellationToken ct = default)
    {
        var fileInfo = GetFileInfo($"data/tournaments/{season.Label}.json");
        if (!fileInfo.Exists)
        {
            return [];
        }

        await using var stream = fileInfo.CreateReadStream();
        return await JsonSerializer.DeserializeAsync<List<TournamentSummaryViewModel>>(stream, s_options, ct) ?? [];
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

    private IFileInfo GetFileInfo(string path) => env.WebRootFileProvider.GetFileInfo(path);
}
