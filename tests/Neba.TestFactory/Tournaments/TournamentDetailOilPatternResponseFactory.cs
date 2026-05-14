using Neba.Api.Contracts.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailOilPatternResponseFactory
{
    private static readonly string[] RoundOptions = ["Qualifying", "Round 1", "Semifinals", "Finals"];
    public const string ValidName = "Kegel Broadway";
    public const int ValidLength = 40;
    public const decimal ValidVolume = 25.0m;
    public const decimal ValidLeftRatio = 3.2m;
    public const decimal ValidRightRatio = 3.1m;

    public static TournamentDetailOilPatternResponse Create(
        string? name = null,
        int? length = null,
        decimal? volume = null,
        decimal? leftRatio = null,
        decimal? rightRatio = null,
        Guid? kegelId = null,
        IReadOnlyCollection<string>? rounds = null)
        => new()
        {
            Name = name ?? ValidName,
            Length = length ?? ValidLength,
            Volume = volume ?? ValidVolume,
            LeftRatio = leftRatio ?? ValidLeftRatio,
            RightRatio = rightRatio ?? ValidRightRatio,
            KegelId = kegelId,
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
                LeftRatio = f.Random.Decimal(1.5m, 5.0m),
                RightRatio = f.Random.Decimal(1.5m, 5.0m),
                KegelId = f.Random.Bool() ? Guid.NewGuid() : null,
                Rounds = [.. f.PickRandom(RoundOptions, f.Random.Int(1, 2))],
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}