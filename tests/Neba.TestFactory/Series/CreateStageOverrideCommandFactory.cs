using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.CreateStageOverride;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

internal static class CreateStageOverrideCommandFactory
{
    public const string ValidReason = "Entry confirmed by the tournament director.";
    public const int ValidVersion = 3;
    public static readonly OverrideKind ValidKind = OverrideKind.Add;

    public static CreateStageOverrideCommand Create(
        SeriesId? seriesId = null,
        SeriesStageId? stageId = null,
        OverrideKind? kind = null,
        BowlerId? bowlerId = null,
        string? reason = null,
        Ulid? callerUserId = null,
        int? version = null)
        => new()
        {
            SeriesId = seriesId ?? SeriesId.New(),
            StageId = stageId ?? SeriesStageId.New(),
            Kind = kind ?? ValidKind,
            BowlerId = bowlerId ?? BowlerId.New(),
            Reason = reason ?? ValidReason,
            CallerUserId = callerUserId ?? Ulid.NewUlid(),
            Version = version ?? ValidVersion,
        };

    internal static IReadOnlyCollection<CreateStageOverrideCommand> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            seriesId: new SeriesId(Ulid.BogusString(faker)),
            stageId: new SeriesStageId(Ulid.BogusString(faker)),
            kind: faker.PickRandom(OverrideKind.Add, OverrideKind.Clear),
            bowlerId: new BowlerId(Ulid.BogusString(faker)),
            reason: faker.Lorem.Sentence(),
            callerUserId: Ulid.Bogus(faker),
            version: faker.Random.Int(0, 50)))];
    }

    public static IReadOnlyCollection<CreateStageOverrideCommand> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}