using ErrorOr;

using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.EditTournament;

internal sealed record EditTournamentCommand
    : ICommand<Updated>
{
    public required TournamentId TournamentId { get; init; }

    public required string Name { get; init; }

    public required TournamentType TournamentType { get; init; }

    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    public required bool StatsEligible { get; init; }

    public required decimal EntryFee { get; init; }

    public required decimal NebaAddedMoney { get; init; }

    public CertificationNumber? BowlingCenterId { get; init; }

    public Uri? ExternalRegistrationUrl { get; init; }

    public StoredFile? Logo { get; init; }

    public OilPatternId? OilPatternId { get; init; }

    public PatternLengthCategory? PatternLengthCategory { get; init; }

    public PatternRatioCategory? PatternRatioCategory { get; init; }

    public DateTimeOffset? OilPatternRevealDateTime { get; init; }
}