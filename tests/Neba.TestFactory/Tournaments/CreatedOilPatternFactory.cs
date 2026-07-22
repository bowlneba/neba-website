using Bogus;

using Neba.Api.Features.Tournaments.CreateOilPattern;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class CreatedOilPatternFactory
{
    public const string ValidName = "Dragon";
    public const int ValidLength = 40;
    public static readonly PatternRatioCategory ValidRatioCategory = PatternRatioCategory.Challenge;

    public static CreatedOilPattern Create(
        OilPatternId? id = null,
        string? name = null,
        int? length = null,
        PatternLengthCategory? lengthCategory = null,
        PatternRatioCategory? ratioCategory = null)
    {
        var resolvedLength = length ?? ValidLength;

        return new CreatedOilPattern
        {
            Id = id ?? OilPatternId.New(),
            Name = name ?? ValidName,
            Length = resolvedLength,
            LengthCategory = lengthCategory ?? PatternLengthCategory.FromLength(resolvedLength),
            RatioCategory = ratioCategory ?? ValidRatioCategory
        };
    }

    internal static IReadOnlyCollection<CreatedOilPattern> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var length = faker.Random.Int(30, 50);

            return new CreatedOilPattern
            {
                Id = new OilPatternId(Ulid.BogusString(faker)),
                Name = faker.Random.Words(2),
                Length = length,
                LengthCategory = PatternLengthCategory.FromLength(length),
                RatioCategory = faker.PickRandom(PatternRatioCategory.List.ToArray())
            };
        })];
    }

    public static IReadOnlyCollection<CreatedOilPattern> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
