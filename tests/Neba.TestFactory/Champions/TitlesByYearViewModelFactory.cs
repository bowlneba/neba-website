using Neba.Website.Server.History.Champions;

namespace Neba.TestFactory.Champions;

public static class TitlesByYearViewModelFactory
{
    public const int ValidYear = 2024;

    public static TitlesByYearViewModel Create(
        int? year = null,
        IReadOnlyCollection<BowlerTitleViewModel>? titles = null)
        => new()
        {
            Year = year ?? ValidYear,
            Titles = titles ?? [BowlerTitleViewModelFactory.Create()],
        };

    public static IReadOnlyCollection<TitlesByYearViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TitlesByYearViewModel>()
            .CustomInstantiator(f => new TitlesByYearViewModel
            {
                Year = f.Date.Past(50).Year,
                Titles = BowlerTitleViewModelFactory.Bogus(f.Random.Int(1, 6), seed),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}