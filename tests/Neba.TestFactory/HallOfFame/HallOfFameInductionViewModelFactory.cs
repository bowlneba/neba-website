using Neba.Api.Features.HallOfFame.Domain;
using Neba.Website.Server.HallOfFame;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionViewModelFactory
{
    public static HallOfFameInductionViewModel Create(
        string? bowlerName = null,
        int? inductionYear = null,
        IReadOnlyCollection<HallOfFameCategory>? categories = null,
        Uri? photoUri = null)
        => new()
        {
            BowlerName = bowlerName ?? "John Doe",
            InductionYear = inductionYear ?? 2020,
            Categories = [.. (categories ?? [HallOfFameCategory.SuperiorPerformance]).Select(c => c.Name)],
            PhotoUri = photoUri
        };

    internal static IReadOnlyCollection<HallOfFameInductionViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new HallOfFameInductionViewModel
        {
            BowlerName = faker.Name.FullName(),
            InductionYear = faker.Date.Past(50).Year,
            Categories = [.. faker.PickRandom(HallOfFameCategory.List).Select(c => c.Name)],
            PhotoUri = new Uri(faker.Internet.Avatar())
        })];
    }

    public static IReadOnlyCollection<HallOfFameInductionViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}