using ErrorOr;

using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.EditTournamentLogo;

internal sealed record EditTournamentLogoCommand
    : ICommand<Updated>
{
    public required TournamentId TournamentId { get; init; }

    public StoredFile? Logo { get; init; }
}