using System.Globalization;

using Bogus;

using Neba.Domain.BowlingCenters;

namespace Neba.TestFactory.BowlingCenters;

public static class CertificationNumberFactory
{
    public const string ValidValue = "01948";
    public const string ValidPlaceholderSequence = "001";

    public static CertificationNumber Create(string? value = null)
        => CertificationNumber.Create(value ?? ValidValue).Value;

    public static CertificationNumber CreatePlaceholder(string? sequence = null)
        => CertificationNumber.Placeholder(sequence ?? ValidPlaceholderSequence).Value;

    public static CertificationNumber Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<CertificationNumber> Bogus(int count, int? seed)
    {
        var faker = new Faker<CertificationNumber>()
            .CustomInstantiator(f => Create(
                value: f.Random.Number(10000, 99999).ToString(CultureInfo.InvariantCulture)
            ));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}