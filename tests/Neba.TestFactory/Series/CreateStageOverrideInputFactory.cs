using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.CreateStageOverride;

namespace Neba.TestFactory.Series;

public static class CreateStageOverrideInputFactory
{
    /// <summary>The Add value.</summary>
    public const int ValidKind = OverrideKindValues.Add;
    public const string ValidReason = "Entry confirmed by the tournament director.";
    public const int ValidVersion = 3;

    public static CreateStageOverrideInput Create(
        int? kind = null,
        string? bowlerId = null,
        string? reason = null,
        int? version = null)
        => new()
        {
            Kind = kind ?? ValidKind,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            Reason = reason ?? ValidReason,
            Version = version ?? ValidVersion,
        };

    internal static IReadOnlyCollection<CreateStageOverrideInput> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            kind: faker.PickRandom(OverrideKindValues.Add, OverrideKindValues.Clear),
            bowlerId: Ulid.BogusString(faker),
            reason: faker.Lorem.Sentence(),
            version: faker.Random.Int(0, 50)))];
    }

    public static IReadOnlyCollection<CreateStageOverrideInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}