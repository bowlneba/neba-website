using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.FileProviders;

using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Server.Tournaments;

internal sealed class TournamentDataService(IWebHostEnvironment env) : ITournamentDataService
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
        var fileInfo = GetFileInfo("data/tournaments/seasons.json");
        if (!fileInfo.Exists)
        {
            return [];
        }

        await using var stream = fileInfo.CreateReadStream();
        var responses = await JsonSerializer.DeserializeAsync<List<SeasonApiResponse>>(stream, s_options, ct) ?? [];
        return responses.ConvertAll(r => new SeasonViewModel
        {
            Id = r.SeasonId,
            Description = r.Description,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
        });
    }

    private IFileInfo GetFileInfo(string path) => env.WebRootFileProvider.GetFileInfo(path);

    private sealed record SeasonApiResponse(string SeasonId, string Description, DateOnly StartDate, DateOnly EndDate);
}