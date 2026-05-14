using Neba.Domain.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class OilPatternFactory
{
    public const string ValidName = "Dragon";
    public const int ValidLength = 40;
    public const decimal ValidVolume = 25.0m;
    public const decimal ValidLeftRatio = 3.0m;
    public const decimal ValidRightRatio = 3.0m;

    public static OilPattern Create(
        OilPatternId? id = null,
        string? name = null,
        int? length = null,
        decimal? volume = null,
        decimal? leftRatio = null,
        decimal? rightRatio = null,
        Guid? kegelId = null)
        => new()
        {
            Id = id ?? OilPatternId.New(),
            Name = name ?? ValidName,
            Length = length ?? ValidLength,
            Volume = volume ?? ValidVolume,
            LeftRatio = leftRatio ?? ValidLeftRatio,
            RightRatio = rightRatio ?? ValidRightRatio,
            KegelId = kegelId
        };

    public static IReadOnlyCollection<OilPattern> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<OilPattern>()
            .CustomInstantiator(f => new()
            {
                Id = new OilPatternId(Ulid.BogusString(f)),
                Name = f.Random.Words(2),
                Length = f.Random.Int(30, 50),
                Volume = f.Random.Decimal(15, 35),
                LeftRatio = f.Random.Decimal(1, 6),
                RightRatio = f.Random.Decimal(1, 6),
                KegelId = f.Random.Bool() ? f.Random.Guid() : null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}