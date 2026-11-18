using ErrorOr;

using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Series;

using Neba.Website.Server.Services;

namespace Neba.Website.Server.Series;

internal sealed class SeriesTournamentOptionsLoader(ApiExecutor apiExecutor, ISeasonsApi seasonsApi, ISeriesApi seriesApi)
{
    /// <summary>
    /// The season's tournaments, minus any held by another Series and cancelled ones, sorted by start date.
    /// Also returns how many were left out, for the grid's note.
    /// </summary>
    public async Task<ErrorOr<(IReadOnlyList<SeriesTournamentOption> Options, int HeldElsewhere)>> LoadAsync(
        string seasonId, string? editingSeriesId, CancellationToken ct = default)
    {
        var tournaments = await apiExecutor.ExecuteAsync("SeasonsApi", "ListTournamentsInSeason",
            token => seasonsApi.ListTournamentsInSeasonAsync(seasonId, token), ct);
        if (tournaments.IsError)
        {
            return tournaments.Errors;
        }

        var allSeries = await apiExecutor.ExecuteAsync("SeriesApi", "ListSeries",
            token => seriesApi.ListSeriesAsync(token), ct);
        if (allSeries.IsError)
        {
            return allSeries.Errors;
        }

        var held = new HashSet<string>(StringComparer.Ordinal);
        foreach (var other in allSeries.Value.Items.Where(s => s.SeasonId == seasonId && s.Id != editingSeriesId))
        {
            var detail = await apiExecutor.ExecuteAsync("SeriesApi", "GetSeries",
                token => seriesApi.GetSeriesAsync(other.Id, token), ct);
            if (detail.IsError)
            {
                return detail.Errors;
            }

            held.UnionWith(detail.Value.Stages.Select(stage => stage.TournamentId));
        }

        IReadOnlyList<SeriesTournamentOption> options = [.. tournaments.Value.Items
            .Where(t => t.Status != "Cancelled" && !held.Contains(t.Id))
            .OrderBy(t => t.StartDate)
            .Select(t => new SeriesTournamentOption(t.Id, SeriesDisplay.TournamentOption(t.StartDate, t.Name), t.StartDate, t.EndDate, t.TeamSize))];

        return (options, held.Count);
    }
}