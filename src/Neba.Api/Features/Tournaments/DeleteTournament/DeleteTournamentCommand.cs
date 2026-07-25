using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed record DeleteTournamentCommand
    : ICommand<Deleted>
{
    public required TournamentId TournamentId { get; init; }
}