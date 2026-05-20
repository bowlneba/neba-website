using System.Globalization;

using Neba.Api.Contacts.Domain;

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

    public static IReadOnlyCollection<PhoneNumber> Bogus(int count, int? seed = null)
    {
        var preFaker = seed.HasValue ? new Faker { Random = new Randomizer(seed.Value) } : new Faker();
        var types = preFaker.Random.Shuffle(PhoneNumberType.List.ToArray()).ToArray();
        var index = 0;

        var faker = new Faker<PhoneNumber>()
            .CustomInstantiator(f =>
            {
                var type = index < types.Length ? types[index++] : f.PickRandom(types);
                return new PhoneNumber
                {
                    Type = type,
                    CountryCode = "1",
                    Number = f.Phone.PhoneNumber("##########"),
                    Extension = f.Random.Bool()
                        ? f.Random.Number(1, 9999).ToString(CultureInfo.InvariantCulture)
                        : null
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}