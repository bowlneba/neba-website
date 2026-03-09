using System.Globalization;

using Bogus;

using Neba.Domain.Contact;

namespace Neba.TestFactory.Contact;

public static class PhoneNumberFactory
{
    public static readonly PhoneNumberType ValidType = PhoneNumberType.Home;
    public const string ValidCountryCode = "1";
    public const string ValidNumber = "5551234567";

    public static PhoneNumber Create(
        PhoneNumberType? type = null,
        string? countryCode = null,
        string? number = null,
        string? extension = null)
         => new()
         {
             Type = type ?? ValidType,
             CountryCode = countryCode ?? ValidCountryCode,
             Number = number ?? ValidNumber,
             Extension = extension
         };

    public static PhoneNumber Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<PhoneNumber> Bogus(int count, int? seed = null)
    {
        var faker = seed.HasValue
            ? new Faker { Random = new Randomizer(seed.Value) }
            : new Faker();

        var types = faker.Random
            .Shuffle(PhoneNumberType.List.ToArray())
            .Take(Math.Min(count, PhoneNumberType.List.Count))
            .ToArray();

        return [.. types.Select(type => Create(
                type: type,
                countryCode: "1",
                number: faker.Phone.PhoneNumber("##########"),
                extension: faker.Random.Bool()
                    ? faker.Random.Number(1, 9999).ToString(CultureInfo.InvariantCulture)
                    : null))];
    }
}