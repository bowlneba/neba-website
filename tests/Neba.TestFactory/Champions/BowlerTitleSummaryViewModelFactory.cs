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

    internal static IReadOnlyCollection<BowlerTitleSummaryViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerTitleSummaryViewModel
        {
            BowlerId = Ulid.BogusString(faker),
            BowlerName = faker.Name.FullName(),
            TitleCount = faker.Random.Int(1, 30),
            HallOfFame = faker.Random.Bool(),
        })];
    }

    public static IReadOnlyCollection<BowlerTitleSummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}