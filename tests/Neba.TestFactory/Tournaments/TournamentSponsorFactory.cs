using Bogus;

using Neba.Domain.Sponsors;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Sponsors;

namespace Neba.TestFactory.Tournaments;

public static class TournamentSponsorFactory
{
    public static TournamentSponsor Create(
        Tournament? tournament = null,
        Sponsor? sponsor = null,
        bool? titleSponsor = null,
        decimal? sponsorshipAmount = null
    )
    {
        var tournamentToUse = tournament ?? TournamentFactory.Create();
        var sponsorToUse = sponsor ?? SponsorFactory.Create();

        return new()
        {
            TournamentId = tournamentToUse.Id,
            Tournament = tournamentToUse,
            SponsorId = sponsorToUse.Id,
            Sponsor = sponsorToUse,
            TitleSponsor = titleSponsor ?? false,
            SponsorshipAmount = sponsorshipAmount ?? 0m,
        };
    }

    public static IReadOnlyCollection<TournamentSponsor> Bogus(
        int count,
        IReadOnlyCollection<Tournament>? tournaments = null,
        IReadOnlyCollection<Sponsor>? sponsors = null,
        int? seed = null)
    {
        var tournamentsToUse = tournaments ?? TournamentFactory.Bogus(count, seed);
        var sponsorsToUse = sponsors ?? SponsorFactory.Bogus(count, seed);

        var faker = new Faker<TournamentSponsor>()
            .CustomInstantiator(f =>
            {
                var tournament = f.PickRandom(tournamentsToUse, 1).Single();
                var sponsor = f.PickRandom(sponsorsToUse, 1).Single();

                return new TournamentSponsor
                {
                    TournamentId = tournament.Id,
                    Tournament = tournament,
                    SponsorId = sponsor.Id,
                    Sponsor = sponsor,
                    TitleSponsor = f.Random.Bool(),
                    SponsorshipAmount = f.Finance.Amount(1000, 100000),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}