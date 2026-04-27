using Bogus;

using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.TestFactory.Tournaments;

public static class SideCutCriteriaFactory
{
    public const int ValidMinimumAge = 50;
    public const int ValidMaximumAge = 17;
    public static readonly Gender ValidGender = Gender.Female;

    /// <summary>
    /// Creates a Criterion representing an age requirement. Defaults to a MinimumAge-only
    /// criterion using <see cref="ValidMinimumAge"/> when both parameters are omitted.
    /// </summary>
    public static SideCutCriteria CreateAgeRequirement(int? minimumAge = null, int? maximumAge = null)
        => SideCutCriteria.CreateAgeRequirement(
            minimumAge ?? (maximumAge.HasValue ? null : ValidMinimumAge),
            maximumAge).Value;

    public static SideCutCriteria CreateGenderRequirement(Gender? gender = null)
        => SideCutCriteria.CreateGenderRequirement(gender ?? ValidGender).Value;

    public static IReadOnlyCollection<SideCutCriteria> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SideCutCriteria>()
            .CustomInstantiator(f => f.Random.Bool()
                ? SideCutCriteria.CreateAgeRequirement(f.Random.Int(1, 65), null).Value
                : SideCutCriteria.CreateGenderRequirement(f.PickRandom(Gender.List.ToArray())).Value);

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}