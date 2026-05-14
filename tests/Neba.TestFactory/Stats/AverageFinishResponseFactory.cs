using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class AverageFinishResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000012";
    public const string ValidBowlerName = "Jane Smith";
    public const decimal ValidAverageFinish = 3.2m;
    public const int ValidFinals = 5;
    public const decimal ValidWinnings = 1500m;

    public static AverageFinishResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        decimal? averageFinish = null,
        int? finals = null,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            AverageFinish = averageFinish ?? ValidAverageFinish,
            Finals = finals ?? ValidFinals,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<AverageFinishResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<AverageFinishResponse>()
            .CustomInstantiator(f => new AverageFinishResponse
            {
                BowlerId = Ulid.BogusString(f),
                BowlerName = f.Name.FullName(),
                AverageFinish = f.Random.Decimal(1, 15),
                Finals = f.Random.Int(1, 10),
                Winnings = f.Random.Decimal(0, 5000)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}