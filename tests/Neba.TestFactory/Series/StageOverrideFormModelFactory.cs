using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class StageOverrideFormModelFactory
{
    public const string ValidReason = "Withdrew before the Stage.";

    /// <summary>A form with a valid reason.</summary>
    public static StageOverrideFormModel Create(string? reason = null)
        => new() { Reason = reason ?? ValidReason };

    public static IReadOnlyList<StageOverrideFormModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(faker.Lorem.Sentence()))];
    }
}