using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.EditTournamentSponsorAmount;

internal sealed record EditTournamentSponsorAmountCommand
    : ICommand
{
    public required TournamentId TournamentId { get; init; }

    public required SponsorId SponsorId { get; init; }

    public required decimal SponsorshipAmount { get; init; }
}