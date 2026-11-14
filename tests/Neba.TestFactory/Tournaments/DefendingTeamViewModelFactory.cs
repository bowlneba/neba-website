using Neba.Website.Server.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class DefendingTeamViewModelFactory
{
    public const string ValidTournamentName = "Joe's Pizza Open";
    public const string ValidSeasonDescription = "2025 Season";

    public static DefendingTeamViewModel Create(
        string? tournamentName = null,
        string? seasonDescription = null,
        bool? sourceTruncated = null,
        bool? undefended = null,
        IReadOnlyList<string>? champions = null)
        => new(
            tournamentName ?? ValidTournamentName,
            seasonDescription ?? ValidSeasonDescription,
            sourceTruncated ?? false,
            undefended ?? false,
            champions ?? ["Dave Smith", "Dana Jones"]);
}
