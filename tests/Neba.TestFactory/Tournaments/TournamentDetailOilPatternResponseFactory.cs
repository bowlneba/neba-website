using Neba.Api.Contracts.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailOilPatternResponseFactory
{
    private static readonly string[] RoundOptions = ["Qualifying", "Round 1", "Semifinals", "Finals"];
    public const string ValidName = "Kegel Broadway";
    public const int ValidLength = 40;
    public const decimal ValidVolume = 22.5m;
    public const decimal ValidLeftRatio = 4.0m;
    public const decimal ValidRightRatio = 6.0m;

    public static TournamentDetailOilPatternResponse Create(
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

    public static IReadOnlyCollection<TournamentDetailOilPatternResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailOilPatternResponse>()
            .CustomInstantiator(f => new()
            {
                Name = f.Lorem.Word() + " " + f.Lorem.Word(),
                Length = f.Random.Int(30, 45),
                Volume = f.Random.Decimal(15, 35),
                LeftRatio = f.Random.Decimal(2, 8),
                RightRatio = f.Random.Decimal(2, 8),
                Rounds = [.. f.PickRandom(RoundOptions, f.Random.Int(1, 2))],
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<TournamentDetailOilPatternResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}