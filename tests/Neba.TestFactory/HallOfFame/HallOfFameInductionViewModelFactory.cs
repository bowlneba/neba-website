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

    public static IReadOnlyCollection<HallOfFameInductionViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<HallOfFameInductionViewModel>()
            .CustomInstantiator(f => new()
            {
                BowlerName = f.Name.FullName(),
                InductionYear = f.Date.Past(50).Year,
                Categories = [.. f.PickRandom(HallOfFameCategory.List).Select(c => c.Name)],
                PhotoUri = new Uri(f.Internet.Avatar())
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}