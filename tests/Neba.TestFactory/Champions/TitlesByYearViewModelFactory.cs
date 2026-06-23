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

    public static IReadOnlyCollection<TitlesByYearViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new TitlesByYearViewModel
        {
            Year = faker.Date.Past(50).Year,
            Titles = BowlerTitleViewModelFactory.Bogus(faker.Random.Int(1, 6), faker),
        })];
    }

    public static IReadOnlyCollection<TitlesByYearViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}