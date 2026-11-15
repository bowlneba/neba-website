using Neba.Api.Features.HallOfFame.Domain;
using Neba.Website.Server.HallOfFame;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionViewModelFactory
{
    public const string ValidId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X";
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W3Z";

    public static HallOfFameInductionViewModel Create(
        string? id = null,
        string? bowlerId = null,
        string? bowlerName = null,
        int? inductionYear = null,
        IReadOnlyCollection<HallOfFameCategory>? categories = null,
        Uri? photoUri = null,
        int? photoFocalX = null,
        int? photoFocalY = null,
        int? photoZoom = null)
        => new()
        {
            Id = id ?? ValidId,
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? "John Doe",
            InductionYear = inductionYear ?? 2020,
            Categories = [.. (categories ?? [HallOfFameCategory.SuperiorPerformance]).Select(c => c.Name)],
            PhotoUri = photoUri,
            PhotoFocalX = photoFocalX,
            PhotoFocalY = photoFocalY,
            PhotoZoom = photoZoom
        };

    internal static IReadOnlyCollection<HallOfFameInductionViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        // A separate randomizer keeps bowler ids from shifting the seeded values the other fields rely on.
        var bowlerIdFaker = new Faker { Random = new Randomizer(count) };

        return [.. Enumerable.Range(0, count).Select(_ => new HallOfFameInductionViewModel
        {
            Id = Ulid.BogusString(faker),
            BowlerId = Ulid.BogusString(bowlerIdFaker),
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