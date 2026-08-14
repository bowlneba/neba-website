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
        IReadOnlyCollection<TournamentSponsor>? sponsors = null,
        IReadOnlyCollection<Squad>? squads = null,
        DateTimeOffset? oilPatternRevealDateTime = null,
        decimal? nebaAddedMoney = null)
    {
        var result = Tournament.Create(
            name: name ?? ValidName,
            tournamentType: tournamentType ?? ValidTournamentType,
            startDate: startDate ?? ValidStartDate,
            endDate: endDate ?? ValidEndDate,
            seasonId: seasonId ?? SeasonId.New(),
            statsEligible: statsEligible ?? true,
            entryFee: entryFee ?? 100m,
            bowlingCenterId: bowlingCenterId,
            externalRegistrationUrl: externalRegistrationUrl,
            logo: logo,
            patternLengthCategory: patternLengthCategory,
            patternRatioCategory: patternRatioCategory,
            oilPatternRevealDateTime: oilPatternRevealDateTime,
            nebaAddedMoney: nebaAddedMoney ?? 0m,
            id: id,
            legacyId: legacyId);

        if (result.IsError)
        {
            throw new InvalidOperationException($"Failed to create tournament: {result.Errors[0].Description}");
        }

        var tournament = result.Value;

        foreach (var tournamentSponsor in sponsors ?? [])
        {
            var addResult = tournament.AddSponsor(tournamentSponsor.SponsorId, tournamentSponsor.TitleSponsor, tournamentSponsor.SponsorshipAmount);

            if (addResult.IsError)
            {
                throw new InvalidOperationException($"Failed to add sponsor with ID {tournamentSponsor.SponsorId} to tournament: {addResult.Errors[0].Description}");
            }
        }

        foreach (var squad in squads ?? [])
        {
            var addResult = tournament.AddSquad(squad.BowlingDateTimeUtc, squad.MaxEntries, squad.LegacyId);

            if (addResult.IsError)
            {
                throw new InvalidOperationException($"Failed to add squad to tournament: {addResult.Errors[0].Description}");
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

            var result = Tournament.Create(
                name: faker.Random.Words(2),
                tournamentType: faker.PickRandom(TournamentType.List.ToArray()),
                startDate: startDate,
                endDate: endDate,
                seasonId: seasons.GetNext().Id,
                statsEligible: faker.Random.Bool(),
                entryFee: faker.Random.Decimal(0, 500),
                bowlingCenterId: certificationNumberPool.GetNextNullable(),
                externalRegistrationUrl: faker.Random.Bool() ? new Uri(faker.Internet.Url()) : null,
                logo: logos.GetNextNullable(),
                patternLengthCategory: faker.Random.Bool() ? faker.PickRandom(PatternLengthCategory.List.ToArray()) : null,
                patternRatioCategory: faker.Random.Bool() ? faker.PickRandom(PatternRatioCategory.List.ToArray()) : null,
                oilPatternRevealDateTime: faker.Random.Bool() ? faker.Date.FutureOffset(1) : null,
                nebaAddedMoney: faker.Random.Decimal(0, 5000),
                id: new TournamentId(Ulid.BogusString(faker)),
                legacyId: faker.Random.Bool() ? faker.Random.Int(1, 9999) : null);

            if (result.IsError)
            {
                throw new InvalidOperationException($"Failed to create tournament: {result.Errors[0].Description}");
            }

            var tournament = result.Value;

            var sponsorCount = faker.Random.Int(0, 2);

            for (var i = 0; i < sponsorCount; i++)
            {
                var sponsor = sponsors.GetNext();
                var titleSponsor = sponsorCount == 1 && faker.Random.Bool(); // only assign title sponsor status if there's one sponsor
                var sponsorshipAmount = faker.Random.Decimal(0, 10000);

                var addResult = tournament.AddSponsor(sponsor.Id, titleSponsor, sponsorshipAmount);

                if (addResult.IsError)
                {
                    throw new InvalidOperationException($"Failed to add sponsor with ID {sponsor.Id} to tournament: {addResult.Errors[0].Description}");
                }
            }

            var squadCount = faker.Random.Int(1, 3);

            for (var i = 0; i < squadCount; i++)
            {
                // Distinct hours keep each squad's bowling date/time unique within the tournament's date range.
                var bowlingDateTimeUtc = new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, 9 + (i * 2), faker.Random.Int(0, 59), 0, TimeSpan.Zero);
                var maxEntries = faker.Random.Bool() ? faker.Random.Int(1, 200) : (int?)null;
                var squadLegacyId = faker.Random.Bool() ? faker.Random.Int(1, 9999) : (int?)null;

                var addSquadResult = tournament.AddSquad(bowlingDateTimeUtc, maxEntries, squadLegacyId);

                if (addSquadResult.IsError)
                {
                    throw new InvalidOperationException($"Failed to add squad to tournament: {addSquadResult.Errors[0].Description}");
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