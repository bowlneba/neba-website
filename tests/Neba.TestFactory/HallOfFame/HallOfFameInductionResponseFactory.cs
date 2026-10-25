using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionResponseFactory
{
    public const int ValidYear = 2024;
    public const string ValidId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X";
    public const string ValidBowlerName = "John Doe";

    // Fixed so seeded Bogus() output stays stable regardless of the day the test runs.
    private static readonly DateOnly RefDate = new(2026, 1, 1);

    public static HallOfFameInductionResponse Create(
        string? id = null,
        int? year = null,
        string? bowlerName = null,
        IReadOnlyCollection<string>? categories = null,
        Uri? photoUri = null,
        int? photoFocalX = null,
        int? photoFocalY = null,
        int? photoZoom = null)
        => new()
        {
            Id = id ?? ValidId,
            Year = year ?? ValidYear,
            BowlerName = bowlerName ?? ValidBowlerName,
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance.Name, HallOfFameCategory.MeritoriousService.Name],
            PhotoUri = photoUri,
            PhotoFocalX = photoFocalX,
            PhotoFocalY = photoFocalY,
            PhotoZoom = photoZoom
        };

    internal static IReadOnlyCollection<HallOfFameInductionResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);

        // A separate randomizer keeps ids from shifting the seeded values the other fields (and their snapshots) rely on.
        var idFaker = new Faker { Random = new Randomizer(poolSeed) };

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var year = faker.Date.PastDateOnly(50, refDate: RefDate).Year;
            var bowlerName = bowlerNamePool.GetNext().ToFormalName();
            var categories = faker.PickRandom(HallOfFameCategory.List.Select(c => c.Name), 2).ToList();
            var photoUri = faker.Random.Bool() ? new Uri(faker.Internet.Url()) : null;

            return new HallOfFameInductionResponse
            {
                Id = Ulid.BogusString(idFaker),
                Year = year,
                BowlerName = bowlerName,
                Categories = categories,
                PhotoUri = photoUri,
                PhotoFocalX = photoUri is null ? null : idFaker.Random.Int(PhotoFocalPoint.Min, PhotoFocalPoint.Max),
                PhotoFocalY = photoUri is null ? null : idFaker.Random.Int(PhotoFocalPoint.Min, PhotoFocalPoint.Max),
                PhotoZoom = photoUri is null ? null : idFaker.Random.Int(PhotoFocalPoint.MinZoom, PhotoFocalPoint.MaxZoom)
            };
        })];
    }

    public static IReadOnlyCollection<HallOfFameInductionResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}