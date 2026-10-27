using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;

/// <summary>
/// Posts the oil pattern reveal to the Discord news channel. Never retried: a retry after a timeout could post twice.
/// </summary>
[NoAutomaticRetry]
internal sealed record AnnounceOilPatternToDiscordJob
    : IBackgroundJob
{
    public required TournamentId TournamentId { get; init; }

    /// <summary>
    /// The reveal time this job was scheduled for. The job exits if the tournament's time has since changed.
    /// </summary>
    public required DateTimeOffset ExpectedRevealDateTime { get; init; }

    public string JobName
        => $"{nameof(AnnounceOilPatternToDiscordJob)}: tournament {TournamentId}";
}
