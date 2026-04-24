using Neba.Domain.BowlingCenters;
using Neba.Domain.Seasons;
using Neba.Domain.Sponsors;
using Neba.Domain.Storage;
using Neba.Domain.Tournaments;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Storage;

namespace Neba.TestFactory.Tournaments;

public static class TournamentFactory
{
    public const string ValidName = "NEBA Singles";
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;
    public static readonly DateOnly ValidStartDate = new(2025, 10, 4);
    public static readonly DateOnly ValidEndDate = new(2025, 10, 5);

    public static Tournament Create(
        TournamentId? id = null,
        string? name = null,
        TournamentType? tournamentType = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CertificationNumber? bowlingCenterId = null,
        PatternRatioCategory? patternRatioCategory = null,
        PatternLengthCategory? patternLengthCategory = null,
        int? legacyId = null,
        SeasonId? seasonId = null,
        decimal? entryFee = null,
        Uri? externalRegistrationUrl = null,
        StoredFile? logo = null,
        IReadOnlyCollection<TournamentSponsor>? sponsors = null)
    {
        var tournament = new Tournament
        {
            Id = id ?? TournamentId.New(),
            Name = name ?? ValidName,
            TournamentType = tournamentType ?? ValidTournamentType,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
            BowlingCenterId = bowlingCenterId,
            PatternRatioCategory = patternRatioCategory,
            PatternLengthCategory = patternLengthCategory,
            LegacyId = legacyId,
            SeasonId = seasonId ?? SeasonId.New(),
            EntryFee = entryFee ?? 100m,
            ExternalRegistrationUrl = externalRegistrationUrl,
            Logo = logo
        };

        foreach (var tournamentSponsor in sponsors ?? [])
        {
            var result = tournament.AddSponsor(tournamentSponsor.SponsorId, tournamentSponsor.TitleSponsor, tournamentSponsor.SponsorshipAmount);

            if (result.IsError)
            {
                throw new InvalidOperationException($"Failed to add sponsor with ID {tournamentSponsor.SponsorId} to tournament: {result.Errors[0].Description}");
            }
        }

        return tournament;
    }

    public static IReadOnlyCollection<Tournament> Bogus(int count, int? seed = null)
    {
        var certificationNumberPool = UniquePool.CreateNullable(CertificationNumberFactory.Bogus(count, seed), seed);
        var seasons = UniquePool.Create(SeasonFactory.Bogus(count, seed: seed), seed);
        var sponsors = UniquePool.Create(SponsorFactory.Bogus(count, seed: seed), seed);
        var logos = UniquePool.CreateNullable(StoredFileFactory.Bogus(count, seed), seed);

        var faker = new Faker<Tournament>()
            .CustomInstantiator(f =>
            {
                var startDate = f.Date.FutureDateOnly(1);
                var endDate = startDate.AddDays(f.Random.Int(0, 1));

                var tournament = new Tournament
                {
                    Id = new TournamentId(Ulid.BogusString(f)),
                    Name = f.Random.Words(2),
                    TournamentType = f.PickRandom(TournamentType.List.ToArray()),
                    StartDate = startDate,
                    EndDate = endDate,
                    BowlingCenterId = certificationNumberPool.GetNextNullable(),
                    PatternRatioCategory = f.Random.Bool() ? f.PickRandom(PatternRatioCategory.List.ToArray()) : null,
                    PatternLengthCategory = f.Random.Bool() ? f.PickRandom(PatternLengthCategory.List.ToArray()) : null,
                    LegacyId = f.Random.Bool() ? f.Random.Int(1, 9999) : null,
                    SeasonId = seasons.GetNext().Id,
                    EntryFee = f.Random.Decimal(0, 500),
                    ExternalRegistrationUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                    Logo = logos.GetNextNullable()
                };

                var sponsorCount = f.Random.Int(0, 2);

                for (var i = 0; i < sponsorCount; i++)
                {
                    var sponsor = sponsors.GetNext();
                    var titleSponsor = sponsorCount == 1 && f.Random.Bool(); // only assign title sponsor status if there's one sponsor
                    var sponsorshipAmount = f.Random.Decimal(0, 10000);

                    var result = tournament.AddSponsor(sponsor.Id, titleSponsor, sponsorshipAmount);

                    if (result.IsError)
                    {
                        throw new InvalidOperationException($"Failed to add sponsor with ID {sponsor.Id} to tournament: {result.Errors[0].Description}");
                    }
                }

                return tournament;
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}