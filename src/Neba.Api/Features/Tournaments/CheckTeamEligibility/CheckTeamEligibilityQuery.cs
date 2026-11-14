using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.CheckTeamEligibility;

/// <summary>
/// Asks whether a team may enter a tournament. Not cached: it is keyed by team and runs rarely.
/// </summary>
internal sealed record CheckTeamEligibilityQuery
    : IQuery<ErrorOr<TeamEligibilityDto>>
{
    public required TournamentId TournamentId { get; init; }

    /// <summary>The team members found in the system.</summary>
    public required IReadOnlyCollection<BowlerId> BowlerIds { get; init; }

    /// <summary>How many team members were not found in the system.</summary>
    public required int UnknownBowlerCount { get; init; }
}
