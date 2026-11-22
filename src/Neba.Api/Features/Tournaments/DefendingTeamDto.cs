using Neba.Api.Contracts.Tournaments;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// A team that may bowl together as a tournament's defending champions, with the tournament it won.
/// Shared by the season list and the tournament detail, which both cache it.
/// </summary>
public sealed record DefendingTeamDto
{
    /// <summary>The name of the tournament the team won.</summary>
    public required string TournamentName { get; init; }

    /// <summary>The season of the tournament the team won.</summary>
    public required string SeasonDescription { get; init; }

    /// <summary>True when the tournament the team won was truncated.</summary>
    public required bool SourceTruncated { get; init; }

    /// <summary>True when the team won an earlier tournament and never got to defend, because a later one was truncated.</summary>
    public required bool Undefended { get; init; }

    /// <summary>The names of the bowlers on the team.</summary>
    public required IReadOnlyCollection<Name> Champions { get; init; }

    /// <summary>How many tournaments in a row the same bowlers won, ending with the one they won last; 1 for a single title.</summary>
    public int ConsecutiveTitles { get; init; } = 1;
}

internal static class DefendingTeamMapping
{
    extension(DefendingTeam team)
    {
        public DefendingTeamDto ToDto()
            => new()
            {
                TournamentName = team.SourceTournamentName,
                SeasonDescription = team.SeasonDescription,
                SourceTruncated = team.SourceTruncated,
                Undefended = team.Undefended,
                Champions = [.. team.Champions.Select(champion => champion.Name)],
                ConsecutiveTitles = team.ConsecutiveTitles
            };
    }

    extension(DefendingTeamDto team)
    {
        public DefendingTeamResponse ToResponse()
            => new()
            {
                TournamentName = team.TournamentName,
                SeasonDescription = team.SeasonDescription,
                SourceTruncated = team.SourceTruncated,
                Undefended = team.Undefended,
                Champions = [.. team.Champions.Select(champion => champion.ToDisplayName())],
                ConsecutiveTitles = team.ConsecutiveTitles
            };
    }
}