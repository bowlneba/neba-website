using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailOilPatternDtoFactory
{
    public const string ValidName = "Test Pattern";
    public const int ValidLength = 40;

    public static TournamentDetailOilPatternDto Create(
        string? name = null,
        int? length = null,
        IReadOnlyCollection<string>? tournamentRounds = null)
        => new()
        {
            Name = name ?? ValidName,
            Length = length ?? ValidLength,
            TournamentRounds = tournamentRounds ?? [TournamentRound.Qualifying.Name],
        };

    public static IReadOnlyCollection<TournamentDetailOilPatternDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailOilPatternDto>()
            .CustomInstantiator(f => new()
            {
                Name = f.Lorem.Word() + " Pattern",
                Length = f.Random.Int(32, 47),
                TournamentRounds = [.. f.PickRandom(TournamentRound.List.ToArray(), f.Random.Int(1, TournamentRound.List.Count)).Select(r => r.Name)],
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}