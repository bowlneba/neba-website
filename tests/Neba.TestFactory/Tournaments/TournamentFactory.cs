using System.Globalization;

using Bogus;

using Neba.Domain.BowlingCenters;
using Neba.Domain.Tournaments;
using Neba.TestFactory.BowlingCenters;

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
        int? legacyId = null)
        => new()
        {
            Id = id ?? TournamentId.New(),
            Name = name ?? ValidName,
            TournamentType = tournamentType ?? ValidTournamentType,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
            BowlingCenterId = bowlingCenterId,
            PatternRatioCategory = patternRatioCategory,
            PatternLengthCategory = patternLengthCategory,
            LegacyId = legacyId
        };

    public static IReadOnlyCollection<Tournament> Bogus(int count, int? seed = null)
    {
        var certificationNumberPool = UniquePool.CreateNullable(CertificationNumberFactory.Bogus(count, seed), seed);
        var faker = new Faker<Tournament>()
            .CustomInstantiator(f =>
            {
                var startDate = f.Date.FutureDateOnly(1);
                var endDate = startDate.AddDays(f.Random.Int(0, 1));

                return new Tournament
                {
                    Id = new TournamentId(Ulid.BogusString(f)),
                    Name = f.Random.Words(2),
                    TournamentType = f.PickRandom(TournamentType.List.ToArray()),
                    StartDate = startDate,
                    EndDate = endDate,
                    BowlingCenterId = certificationNumberPool.GetNextNullable(),
                    PatternRatioCategory = f.Random.Bool() ? f.PickRandom(PatternRatioCategory.List.ToArray()) : null,
                    PatternLengthCategory = f.Random.Bool() ? f.PickRandom(PatternLengthCategory.List.ToArray()) : null,
                    LegacyId = f.Random.Bool() ? f.Random.Int(1, 9999) : null
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}