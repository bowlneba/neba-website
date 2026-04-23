using Neba.Application.Tournaments;
using Neba.Domain.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class TournamentOilPatternDtoFactory
{
    public static TournamentOilPatternDto Create(
        OilPatternDto? oilPattern = null,
        IReadOnlyCollection<TournamentRound>? tournamentRounds = null
    )
    {
        return new TournamentOilPatternDto
        {
            OilPattern = oilPattern ?? OilPatternDtoFactory.Create(),
            TournamentRounds = tournamentRounds?.Select(tournamentRound => tournamentRound.Name).ToList()
                ?? [TournamentRound.Qualifying.Name]
        };
    }

    public static IReadOnlyCollection<TournamentOilPatternDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentOilPatternDto>()
            .CustomInstantiator(f => new TournamentOilPatternDto
            {
                OilPattern = OilPatternDtoFactory.Create(),
                TournamentRounds = [.. f.PickRandom(TournamentRound.List.ToArray(), f.Random.Int(0, TournamentRound.List.Count)).Select(tournamentRound => tournamentRound.Name)]
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}