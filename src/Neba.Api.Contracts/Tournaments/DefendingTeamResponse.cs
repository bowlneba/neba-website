namespace Neba.Api.Contracts.Tournaments;

/// <summary>
/// A team that may bowl together as a tournament's defending champions.
/// </summary>
public sealed record DefendingTeamResponse
{
    /// <summary>
    /// The name of the tournament this team won.
    /// </summary>
    public string TournamentName { get; init; } = string.Empty;

    /// <summary>
    /// The season of the tournament this team won.
    /// </summary>
    public string SeasonDescription { get; init; } = string.Empty;

    /// <summary>
    /// The tournament this team won was truncated.
    /// </summary>
    public bool SourceTruncated { get; init; }

    /// <summary>
    /// The team won an earlier tournament and could not defend because a later one was truncated.
    /// </summary>
    public bool Undefended { get; init; }

    /// <summary>
    /// Display names of the bowlers on the team.
    /// </summary>
    public IReadOnlyCollection<string> Champions { get; init; } = [];
}
