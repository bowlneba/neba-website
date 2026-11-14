using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Tournaments;

public static class DefendingTeamDtoFactory
{
    public const string ValidTournamentName = "Joe's Pizza Open";
    public const string ValidSeasonDescription = "2025 Season";

    public static DefendingTeamDto Create(
        string? tournamentName = null,
        string? seasonDescription = null,
        bool? sourceTruncated = null,
        bool? undefended = null,
        IReadOnlyCollection<Name>? champions = null)
        => new()
        {
            TournamentName = tournamentName ?? ValidTournamentName,
            SeasonDescription = seasonDescription ?? ValidSeasonDescription,
            SourceTruncated = sourceTruncated ?? false,
            Undefended = undefended ?? false,
            Champions = champions ?? [NameFactory.Create(firstName: "Dave", lastName: "Smith"), NameFactory.Create(firstName: "Dana", lastName: "Jones")]
        };
}