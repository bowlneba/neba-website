using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.TruncateTournament;

internal sealed record TruncateTournamentCommand
    : ICommand<Success>
{
    public required TournamentId TournamentId { get; init; }
}