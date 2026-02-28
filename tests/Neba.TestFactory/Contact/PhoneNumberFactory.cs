using System.Globalization;

using Bogus;

using Neba.Domain.Contact;

namespace Neba.TestFactory.Contact;

public static class PhoneNumberFactory
{
    public readonly static PhoneNumberType ValidType = PhoneNumberType.Home;
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
        var faker = new Faker<PhoneNumber>()
            .CustomInstantiator(f => Create(
                type: f.PickRandom(PhoneNumberType.List.ToArray()),
                countryCode: "1",
                number: f.Phone.PhoneNumber("##########"),
                extension: f.Random.Bool() ? f.Random.Number(1, 9999).ToString(CultureInfo.InvariantCulture) : null));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}