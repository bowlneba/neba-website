using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class AverageFinishRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const decimal ValidAverageFinish = 2.5m;
    public const int ValidFinals = 5;
    public const decimal ValidWinnings = 500.00m;

    public static AverageFinishRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        decimal? averageFinish = null,
        int? finals = null,
        decimal? winnings = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? ValidBowlerName,
            AverageFinish = averageFinish ?? ValidAverageFinish,
            Finals = finals ?? ValidFinals,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<AverageFinishRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;

        var faker = new Faker<AverageFinishRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;
                var averageFinish = 1.5m + (currentRank * 0.5m);
                var finals = Math.Max(0, count - currentRank + 1);
                var winnings = (count - currentRank + 1) * 100m;

                return new AverageFinishRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.Bogus(f),
                    BowlerName = f.Name.FullName(),
                    AverageFinish = averageFinish,
                    Finals = finals,
                    Winnings = winnings
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}