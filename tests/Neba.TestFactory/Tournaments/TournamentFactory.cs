using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
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
        bool? statsEligible = null,
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
            StatsEligible = statsEligible ?? true,
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

    internal static IReadOnlyCollection<Tournament> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var certificationNumberPool = UniquePool.CreateNullable(CertificationNumberFactory.Bogus(count, faker), poolSeed);
        var seasons = UniquePool.Create(SeasonFactory.Bogus(count, faker), poolSeed);
        var sponsors = UniquePool.Create(SponsorFactory.Bogus(count, faker), poolSeed);
        var logos = UniquePool.CreateNullable(StoredFileFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var startDate = faker.Date.FutureDateOnly(1);
            var endDate = startDate.AddDays(faker.Random.Int(0, 1));

            var tournament = new Tournament
            {
                Id = new TournamentId(Ulid.BogusString(faker)),
                Name = faker.Random.Words(2),
                TournamentType = faker.PickRandom(TournamentType.List.ToArray()),
                StartDate = startDate,
                EndDate = endDate,
                StatsEligible = faker.Random.Bool(),
                BowlingCenterId = certificationNumberPool.GetNextNullable(),
                PatternRatioCategory = faker.Random.Bool() ? faker.PickRandom(PatternRatioCategory.List.ToArray()) : null,
                PatternLengthCategory = faker.Random.Bool() ? faker.PickRandom(PatternLengthCategory.List.ToArray()) : null,
                LegacyId = faker.Random.Bool() ? faker.Random.Int(1, 9999) : null,
                SeasonId = seasons.GetNext().Id,
                EntryFee = faker.Random.Decimal(0, 500),
                ExternalRegistrationUrl = faker.Random.Bool() ? new Uri(faker.Internet.Url()) : null,
                Logo = logos.GetNextNullable()
            };

            var sponsorCount = faker.Random.Int(0, 2);

            for (var i = 0; i < sponsorCount; i++)
            {
                var sponsor = sponsors.GetNext();
                var titleSponsor = sponsorCount == 1 && faker.Random.Bool(); // only assign title sponsor status if there's one sponsor
                var sponsorshipAmount = faker.Random.Decimal(0, 10000);

                var result = tournament.AddSponsor(sponsor.Id, titleSponsor, sponsorshipAmount);

                if (result.IsError)
                {
                    throw new InvalidOperationException($"Failed to add sponsor with ID {sponsor.Id} to tournament: {result.Errors[0].Description}");
                }
            }

            return tournament;
        })];
    }

    public static IReadOnlyCollection<Tournament> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}