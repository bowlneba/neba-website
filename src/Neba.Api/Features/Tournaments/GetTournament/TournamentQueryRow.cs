using Neba.Api.Features.BowlingCenters.ListBowlingCenters;
using Neba.Api.Features.Seasons.ListSeasons;
using Neba.Api.Features.Sponsors.ListActiveSponsors;
using Neba.Domain.Tournaments;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed partial class GetTournamentQueryHandler
{
    private sealed record TournamentQueryRow
    {
        public int DbId { get; init; }

        public TournamentId Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public SeasonDto Season { get; init; } = null!;

        public DateOnly StartDate { get; init; }

        public DateOnly EndDate { get; init; }

        public bool StatsEligible { get; init; }

        public string TournamentType { get; init; } = string.Empty;

        public TournamentBowlingCenterDto? BowlingCenter { get; init; }

        public IReadOnlyCollection<TournamentSponsorDto> Sponsors { get; init; } = [];

        public decimal? AddedMoney { get; init; }

        public int? Reservations { get; init; }

        public string? PatternLengthCategory { get; init; }

        public string? PatternRatioCategory { get; init; }

        public decimal? EntryFee { get; init; }

        public Uri? RegistrationUrl { get; init; }

        public string? LogoContainer { get; init; }

        public string? LogoPath { get; init; }

        public IReadOnlyCollection<OilPatternRow> OilPatternsRaw { get; init; } = [];
    }
}