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
    public const bool ValidStatsEligible = true;
    public const int ValidPoints = 100;
    public static readonly int? ValidSideCutId;

    public static BoyProgressionResultDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        DateOnly? tournamentDate = null,
        bool? statsEligible = null,
        TournamentType? tournamentType = null,
        int? points = null,
        int? sideCutId = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            TournamentId = tournamentId ?? TournamentId.New(),
            TournamentName = tournamentName ?? ValidTournamentName,
            TournamentDate = tournamentDate ?? ValidTournamentDate,
            StatsEligible = statsEligible ?? ValidStatsEligible,
            TournamentType = tournamentType ?? TournamentType.Singles,
            Points = points ?? ValidPoints,
            SideCutId = sideCutId ?? ValidSideCutId,
        };

    public static IReadOnlyCollection<BoyProgressionResultDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BoyProgressionResultDto>()
            .CustomInstantiator(f => new BoyProgressionResultDto
            {
                BowlerId = BowlerId.Parse(Ulid.BogusString(f, f.Date.Past()), CultureInfo.InvariantCulture),
                BowlerName = new Name { FirstName = f.Person.FirstName, LastName = f.Person.LastName },
                TournamentId = TournamentId.Parse(Ulid.BogusString(f, f.Date.Past()), CultureInfo.InvariantCulture),
                TournamentName = f.Commerce.ProductName(),
                TournamentDate = DateOnly.FromDateTime(f.Date.Past()),
                StatsEligible = f.Random.Bool(),
                TournamentType = f.PickRandom(TournamentType.List.ToArray()),
                Points = f.Random.Int(5, 300),
                SideCutId = f.Random.Bool() ? f.Random.Int(1, 10) : null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}