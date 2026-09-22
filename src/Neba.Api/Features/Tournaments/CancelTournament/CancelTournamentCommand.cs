using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.CancelTournament;

internal sealed record CancelTournamentCommand
    : ICommand<Success>
{
    public required TournamentId TournamentId { get; init; }
}