using ErrorOr;

using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.RemoveTournamentSponsor;

internal sealed record RemoveTournamentSponsorCommand
    : ICommand<Deleted>
{
    public required TournamentId TournamentId { get; init; }

    public required SponsorId SponsorId { get; init; }
}