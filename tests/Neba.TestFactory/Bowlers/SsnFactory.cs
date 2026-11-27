using Bogus;

namespace Neba.TestFactory.Bowlers;

/// <summary>
/// Fake Social Security numbers in the stored form: 9 digits, no dashes. Every one starts with 9,
/// which is never issued as an SSN, so none can belong to a real person.
/// </summary>
public static class SsnFactory
{
    public const string ValidSsn = "900123456";

    public static string Create(string? value = null)
        => value ?? ValidSsn;

    public static IReadOnlyCollection<string> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();

        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => faker.Random.ReplaceNumbers("9########"))];
    }
}