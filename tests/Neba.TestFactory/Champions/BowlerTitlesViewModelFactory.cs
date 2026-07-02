using Neba.Website.Server.History.Champions;

namespace Neba.TestFactory.Champions;

public static class BowlerTitlesViewModelFactory
{
    public const string ValidBowlerName = "Joe Bowler";
    public const bool ValidHallOfFame = false;

    public static BowlerTitlesViewModel Create(
        string? bowlerName = null,
        bool? hallOfFame = null,
        IReadOnlyCollection<TitleViewModel>? titles = null)
        => new()
        {
            BowlerName = bowlerName ?? ValidBowlerName,
            HallOfFame = hallOfFame ?? ValidHallOfFame,
            Titles = titles ?? [TitleViewModelFactory.Create()],
        };

    internal static IReadOnlyCollection<BowlerTitlesViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerTitlesViewModel
        {
            BowlerName = faker.Name.FullName(),
            HallOfFame = faker.Random.Bool(),
            Titles = TitleViewModelFactory.Bogus(faker.Random.Int(1, 10), faker),
        })];
    }

    public static IReadOnlyCollection<BowlerTitlesViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}