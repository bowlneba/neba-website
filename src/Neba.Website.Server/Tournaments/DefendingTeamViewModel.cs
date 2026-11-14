using Neba.Api.Contracts.Tournaments;

namespace Neba.Website.Server.Tournaments;

/// <summary>A team that may bowl together as a tournament's defending champions.</summary>
public sealed record DefendingTeamViewModel(
    string TournamentName,
    string SeasonDescription,
    bool SourceTruncated,
    bool Undefended,
    IReadOnlyList<string> Champions)
{
    /// <summary>Short note under the season, or null for a normal defending team.</summary>
    public string? Note
    {
        get
        {
            if (SourceTruncated)
            {
                return "tournament truncated";
            }

            return Undefended ? "could not defend" : null;
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
            [.. response.Champions]);
    }
}