using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame;
using Neba.Api.Features.Seasons.Domain;

namespace Neba.TestFactory.HallOfFame;

internal static class AwardWinFactory
{
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W2X";
    public const string ValidSeasonId = "01JZ8K3M4N5P6Q7R8S9T0V1W3A";

    public static readonly DateOnly ValidSeasonEndDate = new(2019, 10, 31);

    public static AwardWin Create(
        BowlerId? bowlerId = null,
        SeasonId? seasonId = null,
        DateOnly? seasonEndDate = null,
        BowlerOfTheYearCategory? category = null)
        => new(
            bowlerId ?? new BowlerId(ValidBowlerId),
            seasonId ?? new SeasonId(ValidSeasonId),
            seasonEndDate ?? ValidSeasonEndDate,
            category ?? BowlerOfTheYearCategory.Open);

    public static IReadOnlyCollection<AwardWin> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => new AwardWin(
            new BowlerId(Ulid.BogusString(faker)),
            new SeasonId(Ulid.BogusString(faker)),
            faker.Date.PastDateOnly(refDate: new DateOnly(2026, 1, 1)),
            faker.PickRandom(BowlerOfTheYearCategory.List.ToArray())))];
    }
}
