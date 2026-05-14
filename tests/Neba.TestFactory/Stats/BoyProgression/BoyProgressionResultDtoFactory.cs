using System.Globalization;

using Neba.Application.Stats.BoyProgression;
using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats.BoyProgression;

public static class BoyProgressionResultDtoFactory
{
    public const string ValidTournamentName = "Singles";
    public static readonly DateOnly ValidTournamentDate = new(2025, 1, 15);
    public static readonly DateOnly ValidTournamentEndDate = new(2025, 1, 16);
    public const bool ValidStatsEligible = true;
    public const int ValidPoints = 100;
    public static readonly int? ValidSideCutId;
    public static readonly DateOnly? ValidBowlerDateOfBirth;
    public static readonly Gender? ValidBowlerGender;

    public static BoyProgressionResultDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        DateOnly? bowlerDateOfBirth = null,
        Gender? bowlerGender = null,
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        DateOnly? tournamentEndDate = null,
        bool? statsEligible = null,
        TournamentType? tournamentType = null,
        int? points = null,
        int? sideCutId = null,
        string? sideCutName = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            BowlerDateOfBirth = bowlerDateOfBirth ?? ValidBowlerDateOfBirth,
            BowlerGender = bowlerGender ?? ValidBowlerGender,
            TournamentId = tournamentId ?? TournamentId.New(),
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            TournamentEndDate = tournamentEndDate ?? ValidTournamentEndDate,
            StatsEligible = statsEligible ?? ValidStatsEligible,
            TournamentType = tournamentType ?? TournamentType.Singles,
            Points = points ?? ValidPoints,
            SideCutId = sideCutId ?? ValidSideCutId,
            SideCutName = sideCutName,
        };

    public static IReadOnlyCollection<BoyProgressionResultDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BoyProgressionResultDto>()
            .CustomInstantiator(f => new BoyProgressionResultDto
            {
                BowlerId = BowlerId.Parse(Ulid.BogusString(f, f.Date.Past()), CultureInfo.InvariantCulture),
                BowlerName = new Name { FirstName = f.Person.FirstName, LastName = f.Person.LastName },
                BowlerDateOfBirth = f.Random.Bool() ? DateOnly.FromDateTime(f.Date.Past(yearsToGoBack: 70)) : null,
                BowlerGender = f.Random.Bool() ? f.PickRandom(Gender.List.ToArray()) : null,
                TournamentId = TournamentId.Parse(Ulid.BogusString(f, f.Date.Past()), CultureInfo.InvariantCulture),
                TournamentName = f.Commerce.ProductName(),
                TournamentDate = DateOnly.FromDateTime(f.Date.Past()),
                TournamentEndDate = DateOnly.FromDateTime(f.Date.Past()),
                StatsEligible = f.Random.Bool(),
                TournamentType = f.PickRandom(TournamentType.List.ToArray()),
                Points = f.Random.Int(5, 300),
                SideCutId = f.Random.Bool() ? f.Random.Int(1, 10) : null,
                SideCutName = f.Random.Bool() ? f.PickRandom("Senior", "Super Senior", "Women") : null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}