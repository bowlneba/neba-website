using System.Globalization;

using Neba.Api.Features.BowlingCenters.Domain;

namespace Neba.TestFactory.BowlingCenters;

public static class CertificationNumberFactory
{
    public const string ValidValue = "01948";
    public const string ValidPlaceholderSequence = "001";

    public static CertificationNumber Create(string? value = null)
        => CertificationNumber.Create(value ?? ValidValue).Value;

    public static CertificationNumber CreatePlaceholder(string? sequence = null)
        => CertificationNumber.Placeholder(sequence ?? ValidPlaceholderSequence).Value;

    public static IReadOnlyCollection<CertificationNumber> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
            Create(value: faker.Random.Number(10000, 99999).ToString(CultureInfo.InvariantCulture)))];
    }

    public static IReadOnlyCollection<CertificationNumber> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}