using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class TournamentOilPatternFactory
{
    public static TournamentOilPattern Create(
        OilPatternId? oilPatternId = null,
        IEnumerable<TournamentRound>? tournamentRounds = null)
    {
        var oilPattern = new TournamentOilPattern
        {
            OilPatternId = oilPatternId ?? OilPatternId.New()
        };

        foreach (var round in tournamentRounds ?? [])
        {
            var result = oilPattern.AddTournamentRound(round);
            if (result.IsError)
            {
                throw new InvalidOperationException(
                    $"Failed to add tournament round '{round.Name}' to oil pattern: {result.Errors[0].Description}");
            }
        }

        return oilPattern;
    }

    public static IReadOnlyCollection<TournamentOilPattern> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentOilPattern>()
            .CustomInstantiator(f =>
            {
                var oilPattern = new TournamentOilPattern
                {
                    OilPatternId = new OilPatternId(Ulid.BogusString(f))
                };

                var rounds = f.Random.ListItems(TournamentRound.List.ToList(), f.Random.Int(0, TournamentRound.List.Count));
                foreach (var round in rounds)
                {
                    oilPattern.AddTournamentRound(round);
                }

                return oilPattern;
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}