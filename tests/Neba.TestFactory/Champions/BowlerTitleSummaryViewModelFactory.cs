using Neba.Api.Features.Bowlers.Domain;
using Neba.Website.Server.History.Champions;

namespace Neba.TestFactory.Champions;

public static class BowlerTitleSummaryViewModelFactory
{
    public const string ValidBowlerName = "Joe Bowler";
    public const int ValidTitleCount = 5;
    public const bool ValidHallOfFame = false;

    public static BowlerTitleSummaryViewModel Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? titleCount = null,
        bool? hallOfFame = null)
        => new()
        {
            BowlerId = bowlerId?.Value.ToString() ?? BowlerId.New().Value.ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            TitleCount = titleCount ?? ValidTitleCount,
            HallOfFame = hallOfFame ?? ValidHallOfFame,
        };

    public static IReadOnlyCollection<BowlerTitleSummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerTitleSummaryViewModel>()
            .CustomInstantiator(f => new BowlerTitleSummaryViewModel
            {
                BowlerId = Ulid.BogusString(f),
                BowlerName = f.Name.FullName(),
                TitleCount = f.Random.Int(1, 30),
                HallOfFame = f.Random.Bool(),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}