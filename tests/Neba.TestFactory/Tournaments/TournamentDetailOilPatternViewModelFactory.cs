using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailOilPatternViewModelFactory
{
    private static readonly string[] RoundOptions = ["Qualifying", "Round 1", "Semifinals", "Finals"];
    public const string ValidName = "Kegel Broadway";
    public const int ValidLength = 40;

    public static TournamentDetailOilPatternViewModel Create(
        string? name = null,
        int? length = null,
        IReadOnlyCollection<string>? rounds = null)
        => new()
        {
            Name = name ?? ValidName,
            Length = length ?? ValidLength,
            Rounds = rounds ?? ["Qualifying"],
        };

    public static IReadOnlyCollection<TournamentDetailOilPatternViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailOilPatternViewModel>()
            .CustomInstantiator(f => new TournamentDetailOilPatternViewModel
            {
                Name = f.Lorem.Word() + " " + f.Lorem.Word(),
                Length = f.Random.Int(30, 45),
                Rounds = [.. f.PickRandom(RoundOptions, f.Random.Int(1, 2))],
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
