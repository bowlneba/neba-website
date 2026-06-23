using System.Globalization;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.Api.Features.Tournaments.Domain;
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
    public static readonly string? ValidBowlerGender;

    public static BoyProgressionResultDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        DateOnly? bowlerDateOfBirth = null,
        string? bowlerGender = null,
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        DateOnly? tournamentEndDate = null,
        bool? statsEligible = null,
        int? tournamentType = null,
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
            TournamentType = tournamentType ?? TournamentType.Singles.Value,
            Points = points ?? ValidPoints,
            SideCutId = sideCutId ?? ValidSideCutId,
            SideCutName = sideCutName,
        };

    internal static IReadOnlyCollection<BoyProgressionResultDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BoyProgressionResultDto
        {
            BowlerId = BowlerId.Parse(Ulid.BogusString(faker, faker.Date.Past()), CultureInfo.InvariantCulture),
            BowlerName = new Name { FirstName = faker.Person.FirstName, LastName = faker.Person.LastName },
            BowlerDateOfBirth = faker.Random.Bool() ? DateOnly.FromDateTime(faker.Date.Past(yearsToGoBack: 70)) : null,
            BowlerGender = faker.Random.Bool() ? faker.PickRandom(Gender.List.ToArray()).Value : null,
            TournamentId = TournamentId.Parse(Ulid.BogusString(faker, faker.Date.Past()), CultureInfo.InvariantCulture),
            TournamentName = faker.Commerce.ProductName(),
            TournamentDate = DateOnly.FromDateTime(faker.Date.Past()),
            TournamentEndDate = DateOnly.FromDateTime(faker.Date.Past()),
            StatsEligible = faker.Random.Bool(),
            TournamentType = faker.PickRandom(TournamentType.List.ToArray()).Value,
            Points = faker.Random.Int(5, 300),
            SideCutId = faker.Random.Bool() ? faker.Random.Int(1, 10) : null,
            SideCutName = faker.Random.Bool() ? faker.PickRandom("Senior", "Super Senior", "Women") : null,
        })];
    }

    public static IReadOnlyCollection<BoyProgressionResultDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}