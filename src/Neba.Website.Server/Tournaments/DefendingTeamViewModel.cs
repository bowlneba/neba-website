using Humanizer;

using Neba.Api.Contracts.Tournaments;

namespace Neba.Website.Server.Tournaments;

/// <summary>A team that may bowl together as a tournament's defending champions.</summary>
public sealed record DefendingTeamViewModel(
    string TournamentName,
    string SeasonDescription,
    bool SourceTruncated,
    bool Undefended,
    IReadOnlyList<string> Champions,
    int ConsecutiveTitles = 1)
{
    /// <summary>Short note under the season, or null for a normal defending team that won once.</summary>
    public string? Note
    {
        get
        {
            var titles = ConsecutiveTitles > 1
                ? $"{ConsecutiveTitles}x " + "champion".ToQuantity(Math.Max(1, Champions.Count), ShowQuantityAs.None)
                : null;

            var status = (SourceTruncated, Undefended) switch
            {
                (true, _) => "tournament truncated",
                (_, true) => "could not defend",
                _ => null
            };

            return (titles, status) switch
            {
                (not null, not null) => titles + ", " + status,
                (not null, null) => titles,
                _ => status
            };
        }
    }
}

internal static class DefendingTeamMappingExtensions
{
    extension(DefendingTeamResponse response)
    {
        public DefendingTeamViewModel ToViewModel() => new(
            response.TournamentName,
            response.SeasonDescription,
            response.SourceTruncated,
            response.Undefended,
            [.. response.Champions],
            response.ConsecutiveTitles);
    }
}