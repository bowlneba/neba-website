using Bogus;

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

    public static IReadOnlyCollection<BowlerTitlesViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerTitlesViewModel>()
            .CustomInstantiator(f => new BowlerTitlesViewModel
            {
                BowlerName = f.Name.FullName(),
                HallOfFame = f.Random.Bool(),
                Titles = TitleViewModelFactory.Bogus(f.Random.Int(1, 10), seed),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}