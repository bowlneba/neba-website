using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.AddTournamentSponsor;

internal sealed record AddTournamentSponsorCommand
    : ICommand
{
    public required TournamentId TournamentId { get; init; }

    public required SponsorId SponsorId { get; init; }

    public required bool TitleSponsor { get; init; }

    public required decimal SponsorshipAmount { get; init; }
}