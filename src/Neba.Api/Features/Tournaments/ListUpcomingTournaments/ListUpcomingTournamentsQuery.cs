using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListUpcomingTournaments;

internal sealed record ListUpcomingTournamentsQuery
    : IQuery<IReadOnlyCollection<SeasonTournamentDto>>
{
    public required bool CallerIsAuthenticated { get; init; }

    public required bool CallerHasTournamentManagementPermission { get; init; }
}
