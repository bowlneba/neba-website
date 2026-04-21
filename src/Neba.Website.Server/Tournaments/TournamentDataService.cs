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
        string season, CancellationToken ct = default)
    {
        var fileInfo = GetFileInfo($"data/tournaments/{season}.json");
        if (!fileInfo.Exists)
        {
            return [];
        }

        await using var stream = fileInfo.CreateReadStream();
        return await JsonSerializer.DeserializeAsync<List<TournamentSummaryViewModel>>(stream, s_options, ct) ?? [];
    }

    public async Task<List<string>> GetAvailableSeasonsAsync(CancellationToken ct = default)
    {
        var fileInfo = GetFileInfo("data/tournaments/seasons.json");
        if (!fileInfo.Exists)
        {
            return [];
        }

        await using var stream = fileInfo.CreateReadStream();
        return await JsonSerializer.DeserializeAsync<List<string>>(stream, s_options, ct) ?? [];
    }

    private IFileInfo GetFileInfo(string path) => env.WebRootFileProvider.GetFileInfo(path);
}