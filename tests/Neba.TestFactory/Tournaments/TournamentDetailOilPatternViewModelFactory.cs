using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailOilPatternViewModelFactory
{
    private static readonly string[] RoundOptions = ["Qualifying", "Round 1", "Semifinals", "Finals"];
    public const string ValidName = "Kegel Broadway";
    public const int ValidLength = 40;
    public const decimal ValidVolume = 22.5m;
    public const decimal ValidLeftRatio = 5.2m;
    public const decimal ValidRightRatio = 4.8m;

    public static TournamentDetailOilPatternViewModel Create(
        string? name = null,
        int? length = null,
        decimal? volume = null,
        decimal? leftRatio = null,
        decimal? rightRatio = null,
        IReadOnlyCollection<string>? rounds = null)
        => new()
        {
            Name = name ?? ValidName,
            Length = length ?? ValidLength,
            Volume = volume ?? ValidVolume,
            LeftRatio = leftRatio ?? ValidLeftRatio,
            RightRatio = rightRatio ?? ValidRightRatio,
            Rounds = rounds ?? ["Qualifying"],
        };

    public static IReadOnlyCollection<TournamentDetailOilPatternViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailOilPatternViewModel>()
            .CustomInstantiator(f => new TournamentDetailOilPatternViewModel
            {
                Name = f.Lorem.Word() + " " + f.Lorem.Word(),
                Length = f.Random.Int(30, 45),
                Volume = f.Random.Decimal(15, 30),
                LeftRatio = f.Random.Decimal(3, 8),
                RightRatio = f.Random.Decimal(3, 8),
                Rounds = [.. f.PickRandom(RoundOptions, f.Random.Int(1, 2))],
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<TournamentDetailOilPatternViewModel> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}