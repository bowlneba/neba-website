using Neba.Api.Contracts.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class DefendingTeamResponseFactory
{
    public const string ValidTournamentName = "Joe's Pizza Open";
    public const string ValidSeasonDescription = "2025 Season";

    public static DefendingTeamResponse Create(
        string? tournamentName = null,
        string? seasonDescription = null,
        bool? sourceTruncated = null,
        bool? undefended = null,
        IReadOnlyCollection<string>? champions = null)
        => new()
        {
            TournamentName = tournamentName ?? ValidTournamentName,
            SeasonDescription = seasonDescription ?? ValidSeasonDescription,
            SourceTruncated = sourceTruncated ?? false,
            Undefended = undefended ?? false,
            Champions = champions ?? ["Dave Smith", "Dana Jones"]
        };
}
