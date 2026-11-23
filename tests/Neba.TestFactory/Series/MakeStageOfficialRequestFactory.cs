using Neba.Api.Contracts.Series.MakeStageOfficial;

namespace Neba.TestFactory.Series;

public static class MakeStageOfficialRequestFactory
{
    public static MakeStageOfficialRequest Create(
        string? id = null,
        string? stageId = null,
        MakeStageOfficialInput? official = null)
        => new()
        {
            Id = id ?? Ulid.NewUlid().ToString(),
            StageId = stageId ?? Ulid.NewUlid().ToString(),
            Official = official ?? MakeStageOfficialInputFactory.Create(),
        };

    internal static IReadOnlyCollection<MakeStageOfficialRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            Ulid.BogusString(faker),
            Ulid.BogusString(faker),
            MakeStageOfficialInputFactory.Bogus(1, faker).Single()))];
    }

    public static IReadOnlyCollection<MakeStageOfficialRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
