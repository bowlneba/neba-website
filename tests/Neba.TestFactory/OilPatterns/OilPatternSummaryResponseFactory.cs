using Neba.Api.Contracts.OilPatterns.ListOilPatterns;

namespace Neba.TestFactory.OilPatterns;

public static class OilPatternSummaryResponseFactory
{
    public static OilPatternSummaryResponse Create(
        string? oilPatternId = null,
        string? name = null,
        int? length = null,
        decimal? volume = null,
        decimal? leftRatio = null,
        decimal? rightRatio = null,
        Guid? kegelId = null,
        string? lengthCategory = null,
        string? ratioCategory = null)
        => new()
        {
            OilPatternId = oilPatternId ?? "01J7ZK8X6ZQJ8V3F8N9T9C9R2E",
            Name = name ?? CreateOilPatternRequestFactory.ValidName,
            Length = length ?? CreateOilPatternRequestFactory.ValidLength,
            Volume = volume ?? CreateOilPatternRequestFactory.ValidVolume,
            LeftRatio = leftRatio ?? CreateOilPatternRequestFactory.ValidLeftRatio,
            RightRatio = rightRatio ?? CreateOilPatternRequestFactory.ValidRightRatio,
            KegelId = kegelId,
            LengthCategory = lengthCategory ?? "Medium",
            RatioCategory = ratioCategory ?? "Challenge"
        };

    internal static IReadOnlyCollection<OilPatternSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new OilPatternSummaryResponse
        {
            OilPatternId = Ulid.BogusString(faker),
            Name = faker.Random.Words(2),
            Length = faker.Random.Int(30, 50),
            Volume = faker.Random.Decimal(15, 35),
            LeftRatio = faker.Random.Decimal(1, 6),
            RightRatio = faker.Random.Decimal(1, 6),
            KegelId = faker.Random.Bool() ? faker.Random.Guid() : null,
            LengthCategory = faker.PickRandom("Short", "Medium", "Long"),
            RatioCategory = faker.PickRandom("Sport", "Challenge", "Recreation")
        })];
    }

    public static IReadOnlyCollection<OilPatternSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}