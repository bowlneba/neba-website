using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.HallOfFame.ListHallOfFameInductions;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionDtoFactory
{
    public const int ValidYear = 2025;
    public const string ValidId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X";

    // Fixed so seeded Bogus() output stays stable regardless of the day the test runs.
    private static readonly DateOnly RefDate = new(2026, 1, 1);

    public static HallOfFameInductionDto Create(
        string? id = null,
        int? year = null,
        Name? bowlerName = null,
        IReadOnlyCollection<HallOfFameCategory>? categories = null,
        Uri? photoUri = null,
        int? photoFocalX = null,
        int? photoFocalY = null,
        int? photoZoom = null)
        => new()
        {
            Id = id ?? ValidId,
            Year = year ?? ValidYear,
            BowlerName = bowlerName ?? NameFactory.Create(),
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance],
            PhotoUri = photoUri,
            PhotoFocalX = photoFocalX,
            PhotoFocalY = photoFocalY,
            PhotoZoom = photoZoom
        };

    internal static IReadOnlyCollection<HallOfFameInductionDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var hasPhoto = faker.Random.Bool();
            return new HallOfFameInductionDto
            {
                Id = Ulid.BogusString(faker),
                Year = faker.Date.PastDateOnly(refDate: RefDate).Year,
                BowlerName = bowlerNames.GetNext(),
                Categories = [.. faker.PickRandom(HallOfFameCategory.List, faker.Random.Int(1, HallOfFameCategory.List.Count))],
                PhotoUri = hasPhoto ? new Uri(faker.Internet.Url()) : null,
                PhotoFocalX = hasPhoto ? faker.Random.Int(PhotoFocalPoint.Min, PhotoFocalPoint.Max) : null,
                PhotoFocalY = hasPhoto ? faker.Random.Int(PhotoFocalPoint.Min, PhotoFocalPoint.Max) : null,
                PhotoZoom = hasPhoto ? faker.Random.Int(PhotoFocalPoint.MinZoom, PhotoFocalPoint.MaxZoom) : null
            };
        })];
    }

    public static IReadOnlyCollection<HallOfFameInductionDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}