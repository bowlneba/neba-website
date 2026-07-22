using Bogus;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListOilPatterns;

namespace Neba.TestFactory.Tournaments;

public static class OilPatternSummaryDtoFactory
{
    public const string ValidName = "Dragon";
    public const int ValidLength = 40;
    public const decimal ValidVolume = 25.0m;
    public const decimal ValidLeftRatio = 3.0m;
    public const decimal ValidRightRatio = 3.0m;
    public static readonly PatternLengthCategory ValidLengthCategory = PatternLengthCategory.MediumPattern;
    public static readonly PatternRatioCategory ValidRatioCategory = PatternRatioCategory.Challenge;

    public static OilPatternSummaryDto Create(
        OilPatternId? id = null,
        string? name = null,
        int? length = null,
        decimal? volume = null,
        decimal? leftRatio = null,
        decimal? rightRatio = null,
        Guid? kegelId = null,
        PatternLengthCategory? lengthCategory = null,
        PatternRatioCategory? ratioCategory = null)
        => new()
        {
            Id = id ?? OilPatternId.New(),
            Name = name ?? ValidName,
            Length = length ?? ValidLength,
            Volume = volume ?? ValidVolume,
            LeftRatio = leftRatio ?? ValidLeftRatio,
            RightRatio = rightRatio ?? ValidRightRatio,
            KegelId = kegelId,
            LengthCategory = (lengthCategory ?? ValidLengthCategory).Name,
            RatioCategory = (ratioCategory ?? ValidRatioCategory).Name
        };

    internal static IReadOnlyCollection<OilPatternSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new OilPatternSummaryDto
        {
            Id = new OilPatternId(Ulid.BogusString(faker)),
            Name = faker.Random.Words(2),
            Length = faker.Random.Int(30, 50),
            Volume = faker.Random.Decimal(15, 35),
            LeftRatio = faker.Random.Decimal(1, 6),
            RightRatio = faker.Random.Decimal(1, 6),
            KegelId = faker.Random.Bool() ? faker.Random.Guid() : null,
            LengthCategory = faker.PickRandom(PatternLengthCategory.List.ToArray()).Name,
            RatioCategory = faker.PickRandom(PatternRatioCategory.List.ToArray()).Name
        })];
    }

    public static IReadOnlyCollection<OilPatternSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
